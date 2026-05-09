CREATE TABLE auth_log
(
    id             BIGSERIAL PRIMARY KEY,
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT now(),
    user_id        INTEGER      NULL REFERENCES users (id) ON DELETE SET NULL,
    username       VARCHAR(100) NULL,
    event_type     VARCHAR(32)  NOT NULL,
    failure_reason VARCHAR(64)  NULL,
    ip_address     INET         NULL,
    user_agent     TEXT         NULL
);

CREATE INDEX idx_auth_log_user ON auth_log (user_id, created_at DESC);
CREATE INDEX idx_auth_log_created_at ON auth_log (created_at DESC);
CREATE INDEX idx_auth_log_event_type ON auth_log (event_type, created_at DESC);
