CREATE TABLE competitionlists
(
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(255) NOT NULL,
    plan        INT          NOT NULL,
    specialtyid INT          NOT NULL,
    CONSTRAINT fk_competitionlists_specialties FOREIGN KEY (specialtyid) REFERENCES specialties (id)
);

CREATE TABLE admissioncategories
(
    id                        SERIAL PRIMARY KEY,
    name                      VARCHAR(255) NOT NULL,
    competitionlistid         INT          NOT NULL,
    evaluationcriteriagroupid INT          NOT NULL,
    quota                     INT          NOT NULL,
    priority                  INT          NOT NULL,
    CONSTRAINT fk_admissioncategories_lists  FOREIGN KEY (competitionlistid)         REFERENCES competitionlists (id),
    CONSTRAINT fk_admissioncategories_groups FOREIGN KEY (evaluationcriteriagroupid) REFERENCES evaluationcriteriagroups (id)
);
