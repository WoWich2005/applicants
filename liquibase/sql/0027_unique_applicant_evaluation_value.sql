DELETE FROM applicantevaluationvalues a
USING applicantevaluationvalues b
WHERE a.id > b.id
  AND a.applicantid = b.applicantid
  AND a.evaluationcriteriaid = b.evaluationcriteriaid;

ALTER TABLE applicantevaluationvalues
    ADD CONSTRAINT uq_applicantevaluationvalues_applicant_criteria
    UNIQUE (applicantid, evaluationcriteriaid);
