CREATE TABLE users
(
    id                   SERIAL PRIMARY KEY,
    username             VARCHAR(100) UNIQUE NOT NULL,
    password_hash        VARCHAR(255)        NOT NULL,
    role                 VARCHAR(50)         NOT NULL,
    faculty_id           INTEGER REFERENCES faculties (id),
    is_active            BOOLEAN             NOT NULL DEFAULT true,
    must_change_password BOOLEAN             NOT NULL DEFAULT false,
    created_at           TIMESTAMPTZ         NOT NULL DEFAULT now()
);

CREATE TABLE user_specialty_access
(
    user_id      INTEGER NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    specialty_id INTEGER NOT NULL REFERENCES specialties (id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, specialty_id)
);

CREATE TABLE user_faculty_access
(
    user_id    INTEGER NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    faculty_id INTEGER NOT NULL REFERENCES faculties (id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, faculty_id)
);
