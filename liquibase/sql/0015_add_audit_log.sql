CREATE TABLE audit_log
(
    id          BIGSERIAL PRIMARY KEY,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    user_id     INTEGER     NULL REFERENCES users (id) ON DELETE SET NULL,
    username    VARCHAR(100) NULL,
    role        VARCHAR(50)  NULL,
    action      VARCHAR(32)  NOT NULL,
    entity_type VARCHAR(64)  NOT NULL,
    entity_id   VARCHAR(64)  NOT NULL,
    changes     JSONB        NULL,
    context     JSONB        NULL
);

CREATE INDEX idx_audit_log_entity ON audit_log (entity_type, entity_id, created_at DESC);
CREATE INDEX idx_audit_log_user ON audit_log (user_id, created_at DESC);
CREATE INDEX idx_audit_log_created_at ON audit_log (created_at DESC);
CREATE INDEX idx_audit_log_context_gin ON audit_log USING GIN (context);
