CREATE TABLE IF NOT EXISTS Recipients (
    id uuid NOT NULL PRIMARY KEY,
    wallet_endpoint_reference_version integer NOT NULL,
    wallet_endpoint_reference_endpoint VARCHAR(512) NOT NULL,
    wallet_endpoint_reference_public_key bytea NOT NULL
);
