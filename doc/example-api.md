# Example API for issuing GCs

Below is an example of how the OriginStamp API could look like.

## Issuing a certificate

The issuer can issue a certificate by sending a POST request to the `/issue` endpoint.

POST /issue

```json
{
    "recipient": "f5d8ee39-0f64-4a8f-9d4e-e25b8f3a5b3f",
    "type": "production",
    "quantity": 1200,
    "period": "2022-01-01T00:00:00Z/2022-01-01T01:00:00Z",
    "gridArea": "SE4",
    "attributes": {
        "fuelCode": "T1023431",
        "techCode": "F1023433",
    }
}
```

Accepted 202

```json
{
    "id": "f5d8ee39-0f64-4a8f-9d4e-e25b8f3a5b3f",
}
```

## Status of issuance

The issuer can check the status of the issuance by sending a GET request to the `/issue/{id}` endpoint.

GET /issue/f5d8ee39-0f64-4a8f-9d4e-e25b8f3a5b3f

OK 200

```json
{
    "status": "pending",
    "id": "f5d8ee39-0f64-4a8f-9d4e-e25b8f3a5b3f",
}
```

## Create a recipient

POST /recipients

```json
{
    "walletEndpointReference": {
        "endpoint": "https://example.com/wallet",
        "publicKey": "0x4d8ee39-0f64-4a8f-9d4e-e25b8f3a5b3f",
        "version": 1
    }
}
```

OK 200

```json
{
    "id": "f5d8ee39-0f64-4a8f-9d4e-e25b8f3a5b3f",
}
```
