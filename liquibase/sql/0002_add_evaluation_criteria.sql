CREATE TABLE evaluationcriteria
(
    id       SERIAL PRIMARY KEY,
    name     VARCHAR(255) NOT NULL,
    minvalue INT          NOT NULL,
    maxvalue INT          NOT NULL
);
