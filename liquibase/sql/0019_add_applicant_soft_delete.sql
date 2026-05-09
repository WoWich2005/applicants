CREATE TABLE applicant_deletion_requests
(
    applicant_id         INTEGER PRIMARY KEY REFERENCES applicants (id) ON DELETE CASCADE,
    requested_by_user_id INTEGER     NULL REFERENCES users (id) ON DELETE SET NULL,
    requested_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    request_comment      TEXT        NULL,
    validated_by_user_id INTEGER     NULL REFERENCES users (id) ON DELETE SET NULL,
    validated_at         TIMESTAMPTZ NULL,
    validation_comment   TEXT        NULL,
    status               VARCHAR(20) NOT NULL DEFAULT 'pending'
        CONSTRAINT chk_deletion_request_status CHECK (status IN ('pending', 'confirmed', 'rejected'))
);

CREATE INDEX idx_applicant_deletion_requests_status ON applicant_deletion_requests (status);
