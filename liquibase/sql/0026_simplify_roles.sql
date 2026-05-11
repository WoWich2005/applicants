UPDATE users SET role = 'DataAdministrator' WHERE role = 'FacultyManager';

DROP TABLE IF EXISTS user_specialty_access;
DROP TABLE IF EXISTS user_faculty_access;

ALTER TABLE users DROP COLUMN IF EXISTS faculty_id;
