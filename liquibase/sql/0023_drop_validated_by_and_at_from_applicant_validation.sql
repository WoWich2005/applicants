ALTER TABLE applicant_validation
    DROP COLUMN IF EXISTS validated_by_user_id,
    DROP COLUMN IF EXISTS validated_at,
    DROP COLUMN IF EXISTS last_action_comment;
