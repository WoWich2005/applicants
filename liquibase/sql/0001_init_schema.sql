CREATE TABLE faculties
(
    id   SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

CREATE TABLE specialties
(
    id               SERIAL PRIMARY KEY,
    name             VARCHAR(255) NOT NULL,
    facultyid        INT          NOT NULL,
    recruitmentplan  INT          NOT NULL,
    CONSTRAINT fk_specialties_faculties FOREIGN KEY (facultyid) REFERENCES faculties (id)
);

CREATE TABLE applicants
(
    id   SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

CREATE TABLE documenttypes
(
    id   SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

CREATE TABLE applicantdocuments
(
    id            SERIAL PRIMARY KEY,
    applicantid   INT NOT NULL,
    documentid    INT NOT NULL,
    pointsnumber  INT NOT NULL,
    CONSTRAINT fk_applicantdocuments_applicants    FOREIGN KEY (applicantid) REFERENCES applicants (id),
    CONSTRAINT fk_applicantdocuments_documenttypes FOREIGN KEY (documentid)  REFERENCES documenttypes (id)
);

CREATE TABLE applicantspecialties
(
    id          SERIAL PRIMARY KEY,
    applicantid INT NOT NULL,
    specialtyid INT NOT NULL,
    priority    INT NOT NULL,
    CONSTRAINT fk_applicantspecialties_applicants  FOREIGN KEY (applicantid) REFERENCES applicants (id),
    CONSTRAINT fk_applicantspecialties_specialties FOREIGN KEY (specialtyid) REFERENCES specialties (id)
);
