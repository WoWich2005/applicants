ALTER TABLE applicants
    ADD COLUMN externalid VARCHAR(255) NULL,
    ADD CONSTRAINT uq_applicants_externalid UNIQUE (externalid);

UPDATE applicants SET externalid = CAST(id AS VARCHAR) WHERE externalid IS NULL;

ALTER TABLE applicants
    ALTER COLUMN externalid SET NOT NULL;
