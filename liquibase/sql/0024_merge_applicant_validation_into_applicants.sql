ALTER TABLE applicants ADD COLUMN IF NOT EXISTS validated BOOLEAN NOT NULL DEFAULT false;

UPDATE applicants a
SET validated = true
FROM applicant_validation av
WHERE av.applicant_id = a.id AND av.validated = true;

DROP INDEX IF EXISTS idx_applicant_validation_validated;
DROP TABLE IF EXISTS applicant_validation;
