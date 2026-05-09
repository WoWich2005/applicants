CREATE TABLE applicant_validation
(
    applicant_id         INTEGER PRIMARY KEY REFERENCES applicants (id) ON DELETE CASCADE,
    validated            BOOLEAN     NOT NULL DEFAULT false,
    validated_by_user_id INTEGER     NULL REFERENCES users (id) ON DELETE SET NULL,
    validated_at         TIMESTAMPTZ NULL,
    last_action_comment  TEXT        NULL
);

CREATE INDEX idx_applicant_validation_validated ON applicant_validation (validated);

INSERT INTO applicant_validation (applicant_id, validated)
SELECT id, false
FROM applicants
ON CONFLICT (applicant_id) DO NOTHING;
