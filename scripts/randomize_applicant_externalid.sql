-- Подменяет externalid у всех абитуриентов на случайный уникальный идентификатор.
-- Поле applicants.externalid — VARCHAR(255), NOT NULL, UNIQUE (uq_applicants_externalid).
--
-- Назначение: анонимизация реальных идентификаторов документов (например,
-- перед демонстрацией / скриншотами в дипломной записке).
--
-- ВНИМАНИЕ: операция необратима без бэкапа. Перед запуском убедись, что есть дамп:
--   docker exec bntuapplicants-postgres pg_dump -U bntuapplicants -d bntuapplicants -Fc -f /tmp/b.dump
--
-- Восстановление из бэкапа:
--   docker exec -i bntuapplicants-postgres pg_restore -U bntuapplicants -d bntuapplicants --clean --if-exists < backups/<file>.dump

BEGIN;

-- Генерируем 14-символьный буквенно-цифровой идентификатор (формат, близкий к
-- личному номеру документа), уникальный для каждой строки.
-- Уникальность гарантируется: row_number() даёт разный seed, md5 от random()+id —
-- разные значения; UNIQUE-ограничение дополнительно защитит от коллизий.
UPDATE applicants AS a
SET externalid = upper(substring(
        md5(random()::text || clock_timestamp()::text || a.id::text)
        FROM 1 FOR 14));

-- Контроль: количество строк должно совпадать с числом уникальных externalid.
DO $$
DECLARE
    total      int;
    distinct_n int;
BEGIN
    SELECT count(*), count(DISTINCT externalid) INTO total, distinct_n FROM applicants;
    IF total <> distinct_n THEN
        RAISE EXCEPTION 'Обнаружены дубликаты externalid (% строк, % уникальных) — откат', total, distinct_n;
    END IF;
    RAISE NOTICE 'externalid подменён у % абитуриентов, все значения уникальны', total;
END $$;

COMMIT;
