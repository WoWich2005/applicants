ALTER TABLE applicant_deletion_requests
    DROP COLUMN IF EXISTS requested_at,
    DROP COLUMN IF EXISTS validated_by_user_id;
