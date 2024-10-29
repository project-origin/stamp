# Withdraw flow

The sequence diagram below describes the flow of withdrawing a issued certificate. It works by having Stamp keeping track of all withdrawn certificates. The Vault polls all Stamps for a configured interval and withdraws the certificates in the Vault database. If the certificate, or a part of the certificate, was claimed then the counterpart of the claim is unclaimed.

```mermaid
sequenceDiagram
    participant cl as Client
    participant st as Stamp
    participant re as Registry
    participant wa as Wallet

    cl->>st: POST: withdraw
    st->>re: gRPC: Withdraw
    st-->>cl: 201 Created

    alt Polls all stamps for withdrawn certificates in interval
        wa->>st: GET: certificates/withdrawn?lastWithdrawnId=
        st-->>wa: Withdrawn certificates
        alt Foreach withdrawn certificate
            wa->>wa: Withdraw certificate in DB
            wa->>wa: Get claimed slices of certificate
            alt Foreach claimed slices
                wa->>wa: Get counterpart of claim
                wa->>re: gRPC: Unclaim counterpart
                wa->>wa: Update slice to Available
                wa->>wa: Unclaim Claim
            end
        end
    end

```

This above means Vault needs to know all known Issuers (Stamps) in the network.
