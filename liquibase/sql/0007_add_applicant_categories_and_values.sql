CREATE TABLE applicantadmissioncategories
(
    id                  SERIAL PRIMARY KEY,
    applicantid         INT NOT NULL,
    admissioncategoryid INT NOT NULL,
    selectionpriority   INT NOT NULL,
    CONSTRAINT fk_applicantadmissioncategories_applicants FOREIGN KEY (applicantid)         REFERENCES applicants (id),
    CONSTRAINT fk_applicantadmissioncategories_categories FOREIGN KEY (admissioncategoryid) REFERENCES admissioncategories (id)
);

CREATE TABLE applicantevaluationvalues
(
    id                   SERIAL PRIMARY KEY,
    applicantid          INT NOT NULL,
    evaluationcriteriaid INT NOT NULL,
    value                INT NOT NULL,
    CONSTRAINT fk_applicantevaluationvalues_applicants FOREIGN KEY (applicantid)          REFERENCES applicants (id),
    CONSTRAINT fk_applicantevaluationvalues_criteria   FOREIGN KEY (evaluationcriteriaid) REFERENCES evaluationcriteria (id)
);
