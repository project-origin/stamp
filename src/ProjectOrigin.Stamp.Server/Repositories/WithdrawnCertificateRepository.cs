using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.Repositories;

public interface IWithdrawnCertificateRepository
{
    Task<WithdrawnCertificate> Withdraw(GranularCertificate certificate);
    Task<WithdrawnCertificate?> Get(string registryName, Guid certificateId);
    Task<PageResult<WithdrawnCertificate>> GetMultiple(int fromId, int skip, int limit);
}

public class WithdrawnCertificateRepository : IWithdrawnCertificateRepository
{
    private readonly IDbConnection _connection;

    public WithdrawnCertificateRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<WithdrawnCertificate> Withdraw(GranularCertificate certificate)
    {
        var withdrawnDate = DateTimeOffset.UtcNow;
        await _connection.ExecuteAsync(
            @"INSERT INTO WithdrawnCertificates(certificate_id, registry_name, certificate_type, quantity, start_date, end_date, grid_area, issued_state, rejection_reason, metering_point_id, withdrawn_date)
              VALUES (@certificateId, @registryName, @certificateType, @quantity, @startDate, @endDate, @gridArea, @issuedState, @rejectionReason, @meteringPointId, @withdrawnDate)",
              new
              {
                  certificateId = certificate.Id,
                  certificate.RegistryName,
                  certificate.CertificateType,
                  quantity = (long)certificate.Quantity,
                  certificate.StartDate,
                  certificate.EndDate,
                  certificate.GridArea,
                  certificate.IssuedState,
                  certificate.RejectionReason,
                  certificate.MeteringPointId,
                  withdrawnDate
              }
            );

        foreach (var atr in certificate.ClearTextAttributes)
        {
            await _connection.ExecuteAsync(
                @"INSERT INTO WithdrawnClearTextAttributes(id, attribute_key, attribute_value, certificate_id, registry_name)
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

        foreach (var atr in certificate.HashedAttributes)
        {
            await _connection.ExecuteAsync(
                @"INSERT INTO WithdrawnHashedAttributes(id, attribute_key, attribute_value, salt, certificate_id, registry_name)
                  VALUES (@id, @haKey, @haValue, @salt, @certificateId, @registryName)",
                new
                {
                    id = Guid.NewGuid(),
                    atr.HaKey,
                    atr.HaValue,
                    atr.Salt,
                    certificateId = certificate.Id,
                    certificate.RegistryName
                });
        }

        await _connection.ExecuteAsync(
            @"DELETE FROM ClearTextAttributes WHERE certificate_id = @certificateId AND registry_name = @registryName",
            new { certificateId = certificate.Id, registryName = certificate.RegistryName }
        );
        await _connection.ExecuteAsync(
            @"DELETE FROM HashedAttributes WHERE certificate_id = @certificateId AND registry_name = @registryName",
            new { certificateId = certificate.Id, registryName = certificate.RegistryName }
        );
        await _connection.ExecuteAsync(
            @"DELETE FROM Certificates WHERE id = @certificateId AND registry_name = @registryName",
            new { certificateId = certificate.Id, registryName = certificate.RegistryName }
        );

        return (await Get(certificate.RegistryName, certificate.Id))!;
    }

    public async Task<WithdrawnCertificate?> Get(string registryName, Guid certificateId)
    {
        var wcDictionary = new Dictionary<int, WithdrawnCertificate>();
        await _connection.QueryAsync<WithdrawnCertificate?, CertificateClearTextAttribute?, CertificateHashedAttribute?, WithdrawnCertificate?>(
            @"SELECT wc.*, a.attribute_key as key, a.attribute_value as value, ha.attribute_key as haKey, ha.attribute_value as haValue, ha.salt
              FROM WithdrawnCertificates wc
              LEFT JOIN WithdrawnClearTextAttributes a
                ON wc.certificate_id = a.certificate_id
                AND wc.registry_name = a.registry_name
              LEFT JOIN WithdrawnHashedAttributes ha
                ON wc.certificate_id = ha.certificate_id
                AND wc.registry_name = ha.registry_name
              WHERE wc.certificate_id = @certificateId
                AND wc.registry_name = @registryName",
            (wc, atrClear, atrHashed) =>
            {
                if (wc == null) return null;

                if (!wcDictionary.TryGetValue(wc.Id, out var withdrawnCertificate))
                {
                    withdrawnCertificate = wc;
                    wcDictionary.Add(wc.Id, wc);
                }

                if (atrClear != null && !withdrawnCertificate.ClearTextAttributes.Any(ha => ha.Key == atrClear.Key))
                    withdrawnCertificate.ClearTextAttributes.Add(atrClear.Key, atrClear.Value);

                if (atrHashed != null && !withdrawnCertificate.HashedAttributes.Any(ha => ha.HaKey == atrHashed.HaKey))
                    withdrawnCertificate.HashedAttributes.Add(atrHashed);

                return withdrawnCertificate;
            },
            splitOn: "key, haKey",
            param: new
            {
                certificateId,
                registryName
            });

        return wcDictionary.Values.FirstOrDefault();
    }

    public async Task<PageResult<WithdrawnCertificate>> GetMultiple(int fromId, int skip, int limit)
    {
        string sql = @"CREATE TEMPORARY TABLE withdrawn_work_table ON COMMIT DROP AS (
                            SELECT
                                id,
                                certificate_id,
                                registry_name,
                                certificate_type,
                                quantity,
                                start_date,
                                end_date,
                                grid_area,
                                issued_state,
                                rejection_reason,
                                metering_point_id,
                                withdrawn_date
                            FROM
                                WithdrawnCertificates
                            WHERE
                                id > @fromId
                            ORDER BY
                                id ASC
                        );
                        SELECT count(*) FROM withdrawn_work_table;
                        SELECT * FROM withdrawn_work_table LIMIT @limit OFFSET @skip;";

        using (var gridReader = await _connection.QueryMultipleAsync(sql, new { fromId, limit, skip }))
        {
            var totalCount = await gridReader.ReadSingleAsync<int>();
            var withdrawnCertificates = await gridReader.ReadAsync<WithdrawnCertificate>();

            return new PageResult<WithdrawnCertificate>()
            {
                Items = withdrawnCertificates,
                TotalCount = totalCount,
                Count = withdrawnCertificates.Count(),
                Offset = skip,
                Limit = limit
            };
        }
    }
}
