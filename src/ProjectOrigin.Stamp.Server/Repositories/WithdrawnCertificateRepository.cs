using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.Repositories;

public interface IWithdrawnCertificateRepository
{
    Task<WithdrawnCertificate> Create(string registryName, Guid certificateId);
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

    public async Task<WithdrawnCertificate> Create(string registryName, Guid certificateId)
    {
        var withdrawnDate = DateTimeOffset.UtcNow;
        await _connection.ExecuteAsync(
            @"INSERT INTO WithdrawnCertificates(certificate_id, registry_name, withdrawn_date)
              VALUES (@certificateId, @registryName, @withdrawnDate)",
              new { certificateId, registryName, withdrawnDate }
            );
        return (await Get(registryName, certificateId))!;
    }

    public async Task<WithdrawnCertificate?> Get(string registryName, Guid certificateId)
    {
        return await _connection.QueryFirstOrDefaultAsync<WithdrawnCertificate>(
            @"SELECT id, certificate_id, registry_name, withdrawn_date
              FROM WithdrawnCertificates
              WHERE certificate_id = @certificateId AND registry_name = @registryName",
              new { certificateId, registryName }
            );
    }

    public async Task<PageResult<WithdrawnCertificate>> GetMultiple(int fromId, int skip, int limit)
    {
        string sql = @"CREATE TEMPORARY TABLE withdrawn_work_table ON COMMIT DROP AS (
                            SELECT
                                id,
                                certificate_id,
                                registry_name,
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
