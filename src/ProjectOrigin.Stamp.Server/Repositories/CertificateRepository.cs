using System.Data;
using ProjectOrigin.Stamp.Server.Models;
using System.Threading.Tasks;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectOrigin.Stamp.Server.Repositories;

public interface ICertificateRepository
{
    Task Create(GranularCertificate certificate);
    Task<GranularCertificate?> Get(string registryName, Guid certificateId);
    Task SetState(Guid certificateId, string registryName, IssuedState state, string? rejectionReason = null);
}

public class CertificateRepository : ICertificateRepository
{
    private readonly IDbConnection _connection;

    public CertificateRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task Create(GranularCertificate certificate)
    {
        //TODO Set Hashed attributes
        await _connection.ExecuteAsync(
            @"INSERT INTO Certificates(id, registry_name, certificate_type, quantity, start_date, end_date, grid_area, issued_state, rejection_reason)
              VALUES (@id, @registryName, @certificateType, @quantity, @startDate, @endDate, @gridArea, @issuedState, @rejectionReason)",
            new
            {
                certificate.Id,
                certificate.RegistryName,
                certificate.CertificateType,
                quantity = (long)certificate.Quantity,
                startDate = certificate.StartDate,
                endDate = certificate.EndDate,
                certificate.GridArea,
                certificate.IssuedState,
                certificate.RejectionReason
            });

        foreach (var atr in certificate.ClearTextAttributes)
        {
            await _connection.ExecuteAsync(
                @"INSERT INTO ClearTextAttributes(id, attribute_key, attribute_value, certificate_id, registry_name)
                  VALUES (@id, @key, @value, @certificateId, @registryName)",
                new
                {
                    id = Guid.NewGuid(),
                    atr.Key,
                    atr.Value,
                    certificateId = certificate.Id,
                    certificate.RegistryName
                });
        }
    }

    public async Task<GranularCertificate?> Get(string registryName, Guid certificateId)
    {
        var certsDictionary = new Dictionary<Guid, GranularCertificate>();
        await _connection.QueryAsync<GranularCertificate?, CertificateClearTextAttribute?, GranularCertificate?>(
            @"SELECT c.*, a.attribute_key as key, a.attribute_value as value
              FROM certificates c
              LEFT JOIN ClearTextAttributes a
                ON c.id = a.certificate_id
                AND c.registry_name = a.registry_name
              WHERE c.id = @certificateId
                AND c.registry_name = @registryName",
            (cert, atr) =>
            {
                if (cert == null) return null;

                if (!certsDictionary.TryGetValue(cert.Id, out var certificate))
                {
                    certificate = cert;
                    certsDictionary.Add(cert.Id, cert);
                }

                if(atr != null)
                    certificate.ClearTextAttributes.Add(atr.Key, atr.Value);

                return certificate;
            },
            splitOn: "key",
            param: new
            {
                certificateId,
                registryName
            });

        return certsDictionary.Values.FirstOrDefault();
    }

    public async Task SetState(Guid certificateId, string registryName, IssuedState state, string? rejectionReason = null)
    {
        var rowsChanged = await _connection.ExecuteAsync(
            @"UPDATE Certificates
                SET issued_state = @state,
                    rejection_reason = @rejectionReason 
                WHERE id = @certificateId
                AND registry_name = @registryName",
            new
            {
                certificateId,
                registryName,
                state,
                rejectionReason
            });

        if (rowsChanged != 1)
            throw new InvalidOperationException($"Certificate with id {certificateId} and registry {registryName} could not be found");
    }
}
