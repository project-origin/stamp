# Issuance flow

The sequence diagram below shows the flow of messages between the components when issuing a single certificate. All messages are published to the message broker; the message broker is not shown in the diagram. Before that flow is possible, a recipient must be created in Stamp.

Stamp receives a certificate through an api endpoint. After receiving the certificate, the issuance flow starts with publishing an event. This starts a series of event handlers which is executed in sequence after eachother as modeled below:

```mermaid
sequenceDiagram
    participant cl as Client
    participant st as Stamp
    participant re as Registry
    participant wa as Wallet

    cl->>st: POST: certificate
    st-->>cl: 202 Accepted

    st->>st: CertificateStoredEvent
    st->>re: gRPC: SendCertificate

    st->>st: CertificateSentToRegistryEvent
    alt If Registry accepted the issuance transaction
        st->>st: CertificateIssuedInRegistryEvent
        st->>st: CertificateMarkedAsIssuedEvent
        st->>wa: POST: WalletReceiveRequest
    else If Registry did not accept the issuance transaction
        st->>st: CertificateFailedInRegistryEvent
    end


```

The Registry only allows issuing of certificates from the issuing body and in this case that is Energinet. Hence, a Wallet System is not able to issue certificates. So Stamp domain is responsible for issuing first to the registry and then sending to the wallet. For this to happen, the owner public key on the certificate must be created or calculated by Stamp; the next section describes how the Certificates domain calculates the key.

### Key position when sending certificates/slices to the wallet

A wallet endpoint is needed in order to send slices to the wallet. A position for deriving the child key is needed for each slice sent to the wallet. The position is a integer in the Wallet API. It is a requirement that a new position is used for each new slice.

It is desirable to have a stateless way for getting the position. This is done by depending on the fact that a certificate is unique in a specific time period for a given recipient. So the start date of the certificate period is used as input for calculating the position.

The position calculation algorithm must convert a date into an integer. This is done by calculating the number of minutes elapsed since Jan 1st 2022 (UTC).

_Why calculate since Jan 1st 2022?_ We need a start date, which is before any certificates will be issued. We also want it to be fairly close to when the first certificates will be issued as there is an upper limit for the key position based on this approach.

_What is the upper limit?_ The maximum key position is 2,147,483,647, which is the maximum value of at 32-bit integer. With a resolution of 1 minute, this means that the upper limit is a little more than 4,082 years. Or stated the differently, with the start date defined as above the upper limit is 6105-01-24T02:07:00Z.

_Why 1 minute resolution and not market resolution for electrical measurements?_ The market resolution can change (at the time of writing, it is 1 hour and will soon change to 15 minutes). By setting it to 1 minute, the solution does not care about changes in the market resolution as long as they are full minutes. If the market resolution drops below 1 minute, then this approach does not work.

THERE IS NOTHING hindering that two certificates with same recipient and period are posted thus resulting in same endpoint position.
