ALTER TABLE applicant_deletion_requests
    DROP COLUMN IF EXISTS request_comment,
    DROP COLUMN IF EXISTS validation_comment;
