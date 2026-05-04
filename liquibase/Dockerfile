FROM liquibase/liquibase:5.0.0

RUN lpm add postgresql --global

COPY liquibase.properties /liquibase/liquibase.docker.properties
COPY db.changelog-master.yaml /liquibase/changelog/db.changelog-master.yaml
COPY sql/ /liquibase/changelog/sql/

CMD ["update"]
