DROP TABLE WithdrawnCertificates;

CREATE TABLE WithdrawnCertificates (
    id SERIAL PRIMARY KEY,
    certificate_id UUID NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    certificate_type integer NOT NULL,
    quantity bigint NOT NULL,
    start_date bigint NOT NULL,
    end_date bigint NOT NULL,
    grid_area VARCHAR(256) NOT NULL,
    issued_state integer NOT NULL,
    rejection_reason VARCHAR(1024) NULL,
    metering_point_id VARCHAR(256) NOT NULL,
    withdrawn_date TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS WithdrawnClearTextAttributes (
    id uuid NOT NULL PRIMARY KEY,
    attribute_key VARCHAR(256) NOT NULL,
    attribute_value VARCHAR(512) NOT NULL,
    certificate_id uuid NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    UNIQUE (certificate_id, registry_name, attribute_key)
);

CREATE TABLE IF NOT EXISTS WithdrawnHashedAttributes (
    id uuid NOT NULL PRIMARY KEY,
    attribute_key VARCHAR(256) NOT NULL,
    attribute_value VARCHAR(512) NOT NULL,
    salt bytea NOT NULL,
    certificate_id uuid NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    UNIQUE (certificate_id, registry_name, attribute_key)
);
