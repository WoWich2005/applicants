-- Подменяет externalid у всех абитуриентов на случайный уникальный идентификатор
-- И ЗАЧИЩАЕТ ЕГО СЛЕДЫ В ЖУРНАЛЕ АУДИТА (audit_log.changes).
--
-- Поле applicants.externalid — VARCHAR(255), NOT NULL, UNIQUE (uq_applicants_externalid).
--
-- Назначение: анонимизация реальных идентификаторов документов (например,
-- перед демонстрацией / скриншотами в дипломной записке).
--
-- Почему нужен второй шаг по audit_log: AuditLogger пишет в changes снимки
-- сущности ({after: {externalId: ...}}) и дельты ({diff: {externalId: {old, new}}}).
-- Если подменить только applicants.externalid, реальные номера документов
-- останутся в журнале, а значит и в любом бэкапе БД.
--
-- ВНИМАНИЕ: операция необратима без бэкапа. Перед запуском убедись, что есть дамп:
--   docker exec bntuapplicants-postgres pg_dump -U bntuapplicants -d bntuapplicants -Fc -f /tmp/b.dump
--
-- Восстановление из бэкапа:
--   docker exec -i bntuapplicants-postgres pg_restore -U bntuapplicants -d bntuapplicants --clean --if-exists < backups/<file>.dump
--
-- Запуск:
--   docker exec -i bntuapplicants-postgres psql -U bntuapplicants -d bntuapplicants < scripts/randomize_applicant_externalid.sql

BEGIN;

-- Генерируем 14-символьный идентификатор вида XXXX-XXXX-XXXX (hex в верхнем
-- регистре, группы по четыре символа).
--
-- Дефисы здесь не косметика: реальный формат документа — две буквы и семь цифр
-- подряд ([A-Z]{2}[0-9]{7}), то есть девять символов без разделителя. Группы по
-- четыре делают такой прогон невозможным по построению, поэтому итоговая
-- проверка внизу не может сработать на собственном выхлопе скрипта.
CREATE FUNCTION pg_temp.random_extid() RETURNS text LANGUAGE sql VOLATILE AS $fn$
    SELECT upper(
        substring(h FROM  1 FOR 4) || '-' ||
        substring(h FROM  5 FOR 4) || '-' ||
        substring(h FROM  9 FOR 4))
    FROM (SELECT md5(random()::text || clock_timestamp()::text) AS h) s;
$fn$;

-- ---------------------------------------------------------------------------
-- Шаг 1. Новый externalid для каждого абитуриента. Карта нужна дальше, чтобы в
-- журнале аудита оказалось то же значение, что и в самой таблице.
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE extid_map ON COMMIT DROP AS
SELECT id, pg_temp.random_extid() AS new_extid FROM applicants;

UPDATE applicants a
SET externalid = m.new_extid
FROM extid_map m
WHERE a.id = m.id;

-- ---------------------------------------------------------------------------
-- Шаг 2. Снимки сущности в журнале: changes->'after'->>'externalId'.
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(al.changes, '{after,externalId}', to_jsonb(m.new_extid))
FROM extid_map m
WHERE al.entity_type = 'applicant'
  AND al.entity_id ~ '^\d+$'
  AND al.entity_id::int = m.id
  AND al.changes -> 'after' ? 'externalId';

-- ---------------------------------------------------------------------------
-- Шаг 3. Дельты: changes->'diff'->'externalId'->{old,new}.
-- new получает актуальное значение из карты, old — независимый случайный,
-- чтобы запись в журнале осталась осмысленной («значение менялось»).
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(
        jsonb_set(al.changes, '{diff,externalId,new}', to_jsonb(m.new_extid)),
        '{diff,externalId,old}', to_jsonb(pg_temp.random_extid()))
FROM extid_map m
WHERE al.entity_type = 'applicant'
  AND al.entity_id ~ '^\d+$'
  AND al.entity_id::int = m.id
  AND al.changes -> 'diff' ? 'externalId';

-- ---------------------------------------------------------------------------
-- Шаг 4. Хвосты: записи журнала об абитуриентах, которых в applicants уже нет
-- (подтверждённое удаление). Карта их не покрывает, но реальные номера в них есть.
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(al.changes, '{after,externalId}', to_jsonb(pg_temp.random_extid()))
WHERE al.entity_type = 'applicant'
  AND al.changes -> 'after' ? 'externalId'
  AND NOT EXISTS (
        SELECT 1 FROM extid_map m
        WHERE al.entity_id ~ '^\d+$' AND m.id = al.entity_id::int);

UPDATE audit_log al
SET changes = jsonb_set(
        jsonb_set(al.changes, '{diff,externalId,new}', to_jsonb(pg_temp.random_extid())),
        '{diff,externalId,old}', to_jsonb(pg_temp.random_extid()))
WHERE al.entity_type = 'applicant'
  AND al.changes -> 'diff' ? 'externalId'
  AND NOT EXISTS (
        SELECT 1 FROM extid_map m
        WHERE al.entity_id ~ '^\d+$' AND m.id = al.entity_id::int);

-- ---------------------------------------------------------------------------
-- Контроль: значения уникальны, формата реального документа не осталось нигде.
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    total      int;
    distinct_n int;
    real_table int;
    real_audit int;
BEGIN
    SELECT count(*), count(DISTINCT externalid) INTO total, distinct_n FROM applicants;
    IF total <> distinct_n THEN
        RAISE EXCEPTION 'Обнаружены дубликаты externalid (% строк, % уникальных) — откат', total, distinct_n;
    END IF;

    SELECT count(*) INTO real_table FROM applicants WHERE externalid ~ '^[A-Z]{2}[0-9]{7}$';
    SELECT count(*) INTO real_audit FROM audit_log WHERE changes::text ~ '[A-Z]{2}[0-9]{7}';

    IF real_table > 0 THEN
        RAISE EXCEPTION 'В applicants осталось % номеров реального формата — откат', real_table;
    END IF;
    IF real_audit > 0 THEN
        RAISE EXCEPTION 'В audit_log осталось % номеров реального формата — откат', real_audit;
    END IF;

    RAISE NOTICE 'externalid подменён у % абитуриентов; журнал аудита зачищен', total;
END $$;

COMMIT;
