CREATE TABLE departments
(
    id        SERIAL PRIMARY KEY,
    name      VARCHAR(255) NOT NULL,
    facultyid INT          NOT NULL,
    CONSTRAINT fk_departments_faculties FOREIGN KEY (facultyid) REFERENCES faculties (id)
);
