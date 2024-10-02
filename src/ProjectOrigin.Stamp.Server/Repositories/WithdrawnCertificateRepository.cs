using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectOrigin.Stamp.Server.Models;

namespace ProjectOrigin.Stamp.Server.Repositories;

public interface IWithdrawnCertificateRepository
{
    Task<WithdrawnCertificate> Create(string registryName, Guid certificateId);
    Task<WithdrawnCertificate?> Get(string registryName, Guid certificateId);
    Task<List<WithdrawnCertificate>> GetMultiple(int fromId, int pageSize, int pageNumber);
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

    public async Task<List<WithdrawnCertificate>> GetMultiple(int fromId, int pageSize, int pageNumber)
    {
        var offset = (pageNumber - 1) * pageSize;
        var result = await _connection.QueryAsync<WithdrawnCertificate>(
            @"SELECT id, certificate_id, registry_name, withdrawn_date
              FROM WithdrawnCertificates
              WHERE id > @fromId
              ORDER BY id ASC
              LIMIT @pageSize OFFSET @offset",
              new { fromId, pageSize, offset }
            );

        return result.AsList();
    }
}
