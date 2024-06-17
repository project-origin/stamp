CREATE TABLE IF NOT EXISTS Certificates (
    id uuid NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    certificate_type integer NOT NULL,
    quantity bigint NOT NULL,
    start_date bigint NOT NULL,
    end_date bigint NOT NULL,
    grid_area VARCHAR(256) NOT NULL,
    issued_state integer NOT NULL,
    rejection_reason VARCHAR(1024) NULL,
    PRIMARY KEY(id, registry_name)
);

CREATE TABLE IF NOT EXISTS ClearTextAttributes (
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

CREATE TABLE IF NOT EXISTS HashedAttributes (
    id uuid NOT NULL PRIMARY KEY,
    attribute_key VARCHAR(256) NOT NULL,
    attribute_value VARCHAR(512) NOT NULL,
    salt bytea NOT NULL,
    certificate_id uuid NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    UNIQUE (certificate_id, registry_name, attribute_key),
    FOREIGN KEY (certificate_id, registry_name)
        REFERENCES Certificates (Id, registry_name) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
);

CREATE TABLE IF NOT EXISTS OutboxMessages (
    id uuid NOT NULL PRIMARY KEY,
    message_type VARCHAR(250) NOT NULL,
    json_payload TEXT NOT NULL,
    created timestamp with time zone NOT NULL
);
