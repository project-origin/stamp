using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Stamp.Server.Models;
using GranularCertificateType = ProjectOrigin.Electricity.V1.GranularCertificateType;
using PublicKey = ProjectOrigin.Electricity.V1.PublicKey;

namespace ProjectOrigin.Stamp.Server.Helpers;

public static class Registry
{
    public static class Attributes
    {
        public const string AssetId = "assetId";
    }

    public static IssuedEvent BuildIssuedEvent(string registryName, Guid certificateId, DateInterval period,
        string gridArea, SecretCommitmentInfo commitment, IPublicKey ownerPublicKey,
        GranularCertificateType type, Dictionary<string, string> attributes, List<CertificateHashedAttribute> hashedAttributes)
    {
        var id = new Common.V1.FederatedStreamId
        {
            Registry = registryName,
            StreamId = new Common.V1.Uuid { Value = certificateId.ToString() }
        };

        var issuedEvent = new IssuedEvent
        {
            CertificateId = id,
            Type = type,
            Period = period,
            GridArea = gridArea,
            QuantityCommitment = new Electricity.V1.Commitment
            {
                Content = ByteString.CopyFrom(commitment.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitment.CreateRangeProof(id.StreamId.Value))
            },
            OwnerPublicKey = new PublicKey
            {
                Content = ByteString.CopyFrom(ownerPublicKey.Export()),
                Type = KeyType.Secp256K1
            },
            //AssetIdHash is to be removed as parameter from the registry.
        };
        foreach (var attr in attributes)
        {
            issuedEvent.Attributes.Add(new Electricity.V1.Attribute { Key = attr.Key, Value = attr.Value, Type = AttributeType.Cleartext });
        }

        foreach (var attr in hashedAttributes)
        {
            var str = attr.HaKey + attr.HaValue + certificateId + Convert.ToHexString(attr.Salt);
            var hashedValue = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(str)));
            issuedEvent.Attributes.Add(new Electricity.V1.Attribute { Key = Attributes.AssetId, Value = hashedValue, Type = AttributeType.Hashed });
        }

        return issuedEvent;
    }

    public static Transaction CreateTransaction(this IssuedEvent issuedEvent, IPrivateKey issuerKey)
    {
        var header = new TransactionHeader
        {
            FederatedStreamId = issuedEvent.CertificateId,
            PayloadType = IssuedEvent.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(issuedEvent.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };

        var headerSignature = issuerKey.Sign(header.ToByteArray()).ToArray();

        var transaction = new Transaction
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(headerSignature),
            Payload = issuedEvent.ToByteString()
        };

        return transaction;
    }

    public static string ToShaId(this Transaction transaction) =>
         Convert.ToBase64String(SHA256.HashData(transaction.ToByteArray()));

    public static GranularCertificateType MapToRegistryModel(this Models.GranularCertificateType type) =>
        type switch
        {
            Models.GranularCertificateType.Production => GranularCertificateType.Production,
            Models.GranularCertificateType.Consumption => GranularCertificateType.Consumption,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"GranularCertificateType {type} not supported")
        };
}
