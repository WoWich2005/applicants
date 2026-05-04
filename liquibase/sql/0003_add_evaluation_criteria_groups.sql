CREATE TABLE evaluationcriteriagroups
(
    id   SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

CREATE TABLE evaluationcriteriagroupitems
(
    id          SERIAL PRIMARY KEY,
    groupid     INT NOT NULL,
    criteriaid  INT NOT NULL,
    priority    INT NOT NULL,
    CONSTRAINT fk_groupitems_groups   FOREIGN KEY (groupid)    REFERENCES evaluationcriteriagroups (id),
    CONSTRAINT fk_groupitems_criteria FOREIGN KEY (criteriaid) REFERENCES evaluationcriteria (id)
);
