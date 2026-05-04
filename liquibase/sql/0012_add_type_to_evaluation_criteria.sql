CREATE TYPE criteria_type AS ENUM ('higher_is_better', 'lower_is_better');

ALTER TABLE evaluationcriteria
    ADD COLUMN type criteria_type NOT NULL DEFAULT 'higher_is_better';
