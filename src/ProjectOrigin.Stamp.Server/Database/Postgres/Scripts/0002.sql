CREATE TABLE Certificates (
    id uuid NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    certificate_type integer NOT NULL,
    quantity bigint NOT NULL,
    start_date bigint NOT NULL,
    end_date bigint NOT NULL,
    grid_area VARCHAR(256) NOT NULL,
    PRIMARY KEY(id, registry_name)
);

CREATE TABLE ClearTextAttributes (
    id uuid NOT NULL PRIMARY KEY,
    attribute_key VARCHAR(256) NOT NULL,
    attribute_value VARCHAR(512) NOT NULL,
    certificate_id uuid NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    UNIQUE (certificate_id, registry_name, attribute_key),
    FOREIGN KEY (certificate_id, registry_name)
        REFERENCES Certificates (Id, registry_name) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
);
