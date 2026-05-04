ALTER TABLE specialties
    DROP CONSTRAINT fk_specialties_faculties,
    DROP COLUMN facultyid,
    DROP COLUMN recruitmentplan,
    ADD COLUMN departmentid INT NOT NULL DEFAULT 0;

ALTER TABLE specialties
    ALTER COLUMN departmentid DROP DEFAULT,
    ADD CONSTRAINT fk_specialties_departments FOREIGN KEY (departmentid) REFERENCES departments (id);