CREATE TABLE selectedapplicants
(
    id                  SERIAL PRIMARY KEY,
    applicantid         INT NOT NULL,
    admissioncategoryid INT NOT NULL,
    CONSTRAINT fk_selectedapplicants_applicants
        FOREIGN KEY (applicantid) REFERENCES applicants (id) ON DELETE CASCADE,
    CONSTRAINT fk_selectedapplicants_admissioncategories
        FOREIGN KEY (admissioncategoryid) REFERENCES admissioncategories (id) ON DELETE CASCADE,
    CONSTRAINT uq_selectedapplicants UNIQUE (applicantid, admissioncategoryid)
);

CREATE INDEX idx_selectedapplicants_category  ON selectedapplicants (admissioncategoryid);
CREATE INDEX idx_selectedapplicants_applicant ON selectedapplicants (applicantid);
