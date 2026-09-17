-- Подменяет имена абитуриентов (applicants.name) на правдоподобные ФИО
-- И ЗАЧИЩАЕТ ИХ СЛЕДЫ В ЖУРНАЛЕ АУДИТА (audit_log.changes).
--
-- Формат: «Фамилия Имя Отчество», русско-белорусские имена.
-- Пол выбирается случайно; фамилия, имя и отчество согласованы по полу
-- (женские фамилии -ова/-ская, женские отчества -овна/-евна).
--
-- На name нет UNIQUE-ограничения, поэтому повторы допустимы (как в реальности).
--
-- Почему нужен второй шаг по audit_log: AuditLogger пишет в changes снимки
-- сущности ({after: {name: ...}}) и дельты ({diff: {name: {old, new}}}).
-- Если подменить только applicants.name, реальные ФИО останутся в журнале,
-- а значит и в любом бэкапе БД.
--
-- ВНИМАНИЕ: операция необратима без бэкапа. Восстановление:
--   docker exec -i bntuapplicants-postgres pg_restore -U bntuapplicants -d bntuapplicants --clean --if-exists < backups/<file>.dump
--
-- Запуск:
--   docker exec -i bntuapplicants-postgres psql -U bntuapplicants -d bntuapplicants < scripts/randomize_applicant_names.sql

BEGIN;

-- ---------------------------------------------------------------------------
-- Пул имён. Вынесен в таблицу, а не в inline-массивы: из него берёт значения
-- генератор и по нему же проверяется результат в финальном блоке контроля.
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE fio_pool (kind text, gender text, value text) ON COMMIT DROP;

INSERT INTO fio_pool (kind, gender, value)
SELECT 'surname', 'm', unnest(ARRAY[
    'Иванов','Петров','Козлов','Новиков','Морозов','Волков','Соколов',
    'Лебедев','Кузнецов','Васильев','Павлов','Голубев','Богданов','Ковалёв',
    'Зайцев','Дроздов','Луканский','Барановский','Савицкий','Карпович',
    'Наркович','Тарасевич','Гоч','Шайтор','Лобань','Бондарь','Мельник']);

INSERT INTO fio_pool (kind, gender, value)
SELECT 'surname', 'f', unnest(ARRAY[
    'Иванова','Петрова','Козлова','Новикова','Морозова','Волкова','Соколова',
    'Лебедева','Кузнецова','Васильева','Павлова','Голубева','Богданова','Ковалёва',
    'Зайцева','Дроздова','Луканская','Барановская','Савицкая','Карпович',
    'Наркович','Тарасевич','Гоч','Шайтор','Лобань','Бондарь','Мельник']);

INSERT INTO fio_pool (kind, gender, value)
SELECT 'given', 'm', unnest(ARRAY[
    'Александр','Дмитрий','Сергей','Андрей','Максим','Артём','Иван','Никита',
    'Кирилл','Егор','Илья','Михаил','Владислав','Павел','Антон','Глеб','Роман',
    'Денис','Алексей','Тимофей','Владимир','Георгий','Вадим']);

INSERT INTO fio_pool (kind, gender, value)
SELECT 'given', 'f', unnest(ARRAY[
    'Анна','Мария','Виктория','Полина','Анастасия','Дарья','Екатерина',
    'Елизавета','Ксения','Алина','Ольга','Юлия','Валерия','София','Татьяна',
    'Маргарита','Вероника','Кристина','Арина','Диана','Ангелина','Надежда']);

INSERT INTO fio_pool (kind, gender, value)
SELECT 'patronymic', 'm', unnest(ARRAY[
    'Иванович','Дмитриевич','Сергеевич','Александрович','Викторович',
    'Михайлович','Андреевич','Николаевич','Алексеевич','Владимирович',
    'Григорьевич','Олегович','Павлович','Юрьевич','Евгеньевич','Анатольевич',
    'Васильевич','Петрович','Максимович','Артёмович','Денисович','Игоревич']);

INSERT INTO fio_pool (kind, gender, value)
SELECT 'patronymic', 'f', unnest(ARRAY[
    'Ивановна','Дмитриевна','Сергеевна','Александровна','Викторовна',
    'Михайловна','Андреевна','Николаевна','Алексеевна','Владимировна',
    'Григорьевна','Олеговна','Павловна','Юрьевна','Евгеньевна','Анатольевна',
    'Васильевна','Петровна','Максимовна','Артёмовна','Денисовна','Игоревна']);

CREATE FUNCTION pg_temp.random_fio() RETURNS text LANGUAGE plpgsql VOLATILE AS $fn$
DECLARE
    g          text;
    surname    text;
    given      text;
    patronymic text;
BEGIN
    g := CASE WHEN random() < 0.5 THEN 'm' ELSE 'f' END;
    SELECT value INTO surname    FROM fio_pool WHERE kind = 'surname'    AND gender = g ORDER BY random() LIMIT 1;
    SELECT value INTO given      FROM fio_pool WHERE kind = 'given'      AND gender = g ORDER BY random() LIMIT 1;
    SELECT value INTO patronymic FROM fio_pool WHERE kind = 'patronymic' AND gender = g ORDER BY random() LIMIT 1;
    RETURN surname || ' ' || given || ' ' || patronymic;
END $fn$;

-- ---------------------------------------------------------------------------
-- Шаг 1. Новое имя для каждого абитуриента. Карта нужна дальше, чтобы в
-- журнале аудита оказалось то же имя, что и в самой таблице.
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE name_map ON COMMIT DROP AS
SELECT id, pg_temp.random_fio() AS new_name FROM applicants;

UPDATE applicants a
SET name = m.new_name
FROM name_map m
WHERE a.id = m.id;

-- ---------------------------------------------------------------------------
-- Шаг 2. Снимки сущности в журнале: changes->'after'->>'name'.
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(al.changes, '{after,name}', to_jsonb(m.new_name))
FROM name_map m
WHERE al.entity_type = 'applicant'
  AND al.entity_id ~ '^\d+$'
  AND al.entity_id::int = m.id
  AND al.changes -> 'after' ? 'name';

-- ---------------------------------------------------------------------------
-- Шаг 3. Дельты: changes->'diff'->'name'->{old,new}.
-- new получает актуальное имя из карты, old — независимый случайный вариант,
-- чтобы запись в журнале осталась осмысленной («значение менялось»).
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(
        jsonb_set(al.changes, '{diff,name,new}', to_jsonb(m.new_name)),
        '{diff,name,old}', to_jsonb(pg_temp.random_fio()))
FROM name_map m
WHERE al.entity_type = 'applicant'
  AND al.entity_id ~ '^\d+$'
  AND al.entity_id::int = m.id
  AND al.changes -> 'diff' ? 'name';

-- ---------------------------------------------------------------------------
-- Шаг 4. Хвосты: записи журнала об абитуриентах, которых в applicants уже нет
-- (подтверждённое удаление). Карта их не покрывает, но реальные ФИО в них есть.
-- ---------------------------------------------------------------------------
UPDATE audit_log al
SET changes = jsonb_set(al.changes, '{after,name}', to_jsonb(pg_temp.random_fio()))
WHERE al.entity_type = 'applicant'
  AND al.changes -> 'after' ? 'name'
  AND NOT EXISTS (
        SELECT 1 FROM name_map m
        WHERE al.entity_id ~ '^\d+$' AND m.id = al.entity_id::int);

UPDATE audit_log al
SET changes = jsonb_set(
        jsonb_set(al.changes, '{diff,name,new}', to_jsonb(pg_temp.random_fio())),
        '{diff,name,old}', to_jsonb(pg_temp.random_fio()))
WHERE al.entity_type = 'applicant'
  AND al.changes -> 'diff' ? 'name'
  AND NOT EXISTS (
        SELECT 1 FROM name_map m
        WHERE al.entity_id ~ '^\d+$' AND m.id = al.entity_id::int);

-- ---------------------------------------------------------------------------
-- Контроль: ни одной фамилии вне пула — ни в таблице, ни в журнале.
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    n              int;
    alien_table    int;
    alien_audit    int;
BEGIN
    SELECT count(*) INTO n FROM applicants;

    SELECT count(*) INTO alien_table
    FROM applicants
    WHERE split_part(name, ' ', 1) NOT IN (SELECT value FROM fio_pool WHERE kind = 'surname');

    SELECT count(*) INTO alien_audit
    FROM audit_log al
    CROSS JOIN LATERAL (
        SELECT (regexp_matches(al.changes::text,
                '([А-ЯЁ][а-яё]+) [А-ЯЁ][а-яё]+ [А-ЯЁ][а-яё]+', 'g'))[1] AS surname
    ) x
    WHERE x.surname NOT IN (SELECT value FROM fio_pool WHERE kind = 'surname');

    IF alien_table > 0 THEN
        RAISE EXCEPTION 'В applicants осталось % имён вне пула — откат', alien_table;
    END IF;
    IF alien_audit > 0 THEN
        RAISE EXCEPTION 'В audit_log осталось % упоминаний ФИО вне пула — откат', alien_audit;
    END IF;

    RAISE NOTICE 'Имена подменены у % абитуриентов; журнал аудита зачищен', n;
END $$;

COMMIT;
