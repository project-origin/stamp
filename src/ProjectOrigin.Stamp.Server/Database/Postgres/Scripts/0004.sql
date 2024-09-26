CREATE TABLE IF NOT EXISTS WithdrawnCertificates (
    id SERIAL PRIMARY KEY,
    certificate_id UUID NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    withdrawn_date TIMESTAMP WITH TIME ZONE NOT NULL
)
