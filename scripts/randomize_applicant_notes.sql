-- Подменяет заметки абитуриентов (applicants.notes) на случайные телефоны
-- И ЗАЧИЩАЕТ ИХ СЛЕДЫ В ЖУРНАЛЕ АУДИТА (audit_log.changes).
--
-- Поле notes на практике используется как контактный телефон абитуриента:
-- подавляющее большинство значений имеет вид 375(29)1234567. Это персональные
-- данные ровно в той же степени, что ФИО и номер документа, поэтому перед
-- выгрузкой дампа в репозиторий поле должно быть обезличено.
--
-- Скрипт заменяет ВСЕ непустые notes, включая единичные значения другого вида
-- (произвольный текст, телефон с пробелами) — распознавать их по отдельности
-- смысла нет, любое из них может содержать реальные данные.
--
-- Почему нужен второй шаг по audit_log: AuditLogger пишет в changes снимок
-- сущности ({after: {notes: ...}}). Если подменить только applicants.notes,
-- реальные телефоны останутся в журнале, а значит и в любом бэкапе БД.
--
-- ВНИМАНИЕ: операция необратима без бэкапа. Восстановление:
--   docker exec -i bntuapplicants-postgres pg_restore -U bntuapplicants -d bntuapplicants --clean --if-exists < backups/<file>.dump
--
-- Запуск:
--   docker exec -i bntuapplicants-postgres psql -U bntuapplicants -d bntuapplicants < scripts/randomize_applicant_notes.sql

BEGIN;

-- Код оператора — из реально существующих в РБ, чтобы значение выглядело
-- правдоподобно на скриншотах; сам номер случаен.
CREATE FUNCTION pg_temp.random_phone() RETURNS text LANGUAGE sql VOLATILE AS $fn$
    SELECT '375('
        || (ARRAY['25','29','33','44'])[1 + floor(random() * 4)::int]
        || ')'
        || lpad(floor(random() * 10000000)::bigint::text, 7, '0');
$fn$;

-- Снимок старых значений — нужен только для финальной проверки, что ни одно
-- реальное значение не уцелело.
CREATE TEMP TABLE notes_before ON COMMIT DROP AS
SELECT DISTINCT notes FROM applicants WHERE notes IS NOT NULL AND notes <> '';

-- ---------------------------------------------------------------------------
-- Шаг 1. Новая заметка для каждого абитуриента, у которого она была.
-- Пустые и NULL не трогаем: их отсутствие само по себе не персональные данные.
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE notes_map ON COMMIT DROP AS
SELECT id, pg_temp.random_phone() AS new_notes
FROM applicants
WHERE notes IS NOT NULL AND notes <> '';

UPDATE applicants a
SET notes = m.new_notes
FROM notes_map m
WHERE a.id = m.id;

-- ---------------------------------------------------------------------------
-- Шаг 2. Снимки сущности в журнале: changes->'after'->>'notes'.
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(al.changes, '{after,notes}', to_jsonb(m.new_notes))
FROM notes_map m
WHERE al.entity_type = 'applicant'
  AND al.entity_id ~ '^\d+$'
  AND al.entity_id::int = m.id
  AND al.changes -> 'after' ? 'notes'
  AND jsonb_typeof(al.changes -> 'after' -> 'notes') = 'string';

-- ---------------------------------------------------------------------------
-- Шаг 3. Дельты: changes->'diff'->'notes'->{old,new}.
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(
        jsonb_set(al.changes, '{diff,notes,new}', to_jsonb(m.new_notes)),
        '{diff,notes,old}', to_jsonb(pg_temp.random_phone()))
FROM notes_map m
WHERE al.entity_type = 'applicant'
  AND al.entity_id ~ '^\d+$'
  AND al.entity_id::int = m.id
  AND al.changes -> 'diff' ? 'notes';

-- ---------------------------------------------------------------------------
-- Шаг 4. Хвосты: записи журнала об абитуриентах, которых в applicants уже нет
-- (подтверждённое удаление), либо у которых notes успели очистить.
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(al.changes, '{after,notes}', to_jsonb(pg_temp.random_phone()))
WHERE al.entity_type = 'applicant'
  AND al.changes -> 'after' ? 'notes'
  AND jsonb_typeof(al.changes -> 'after' -> 'notes') = 'string'
  AND NOT EXISTS (
        SELECT 1 FROM notes_map m
        WHERE al.entity_id ~ '^\d+$' AND m.id = al.entity_id::int);

UPDATE audit_log al
SET changes = jsonb_set(
        jsonb_set(al.changes, '{diff,notes,new}', to_jsonb(pg_temp.random_phone())),
        '{diff,notes,old}', to_jsonb(pg_temp.random_phone()))
WHERE al.entity_type = 'applicant'
  AND al.changes -> 'diff' ? 'notes'
  AND NOT EXISTS (
        SELECT 1 FROM notes_map m
        WHERE al.entity_id ~ '^\d+$' AND m.id = al.entity_id::int);

-- ---------------------------------------------------------------------------
-- Контроль: ни одно исходное значение не уцелело ни в таблице, ни в журнале.
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    n            int;
    left_table   int;
    left_audit   int;
BEGIN
    SELECT count(*) INTO n FROM notes_map;

    SELECT count(*) INTO left_table
    FROM applicants a
    JOIN notes_before b ON b.notes = a.notes;

    SELECT count(*) INTO left_audit
    FROM audit_log al
    JOIN notes_before b ON al.changes::text LIKE '%' || b.notes || '%';

    IF left_table > 0 THEN
        RAISE EXCEPTION 'В applicants уцелело % исходных значений notes — откат', left_table;
    END IF;
    IF left_audit > 0 THEN
        RAISE EXCEPTION 'В audit_log уцелело % исходных значений notes — откат', left_audit;
    END IF;

    RAISE NOTICE 'notes подменены у % абитуриентов; журнал аудита зачищен', n;
END $$;

COMMIT;
