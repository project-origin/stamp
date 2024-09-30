using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectOrigin.Stamp.Models;

namespace ProjectOrigin.Stamp.Repositories;

public interface IWithdrawnCertificateRepository
{
    Task Create(string registryName, Guid certificateId);
    Task<WithdrawnCertificate?> Get(string registryName, Guid certificateId);
}

public class WithdrawnCertificateRepository : IWithdrawnCertificateRepository
{
    private readonly IDbConnection _connection;

    public WithdrawnCertificateRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task Create(string registryName, Guid certificateId)
    {
        var withdrawnDate = DateTimeOffset.UtcNow;
        await _connection.ExecuteAsync(
            @"INSERT INTO WithdrawnCertificates(certificate_id, registry_name, withdrawn_date)
              VALUES (@certificateId, @registryName, @withdrawnDate)",
              new { certificateId, registryName, withdrawnDate }
            );
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
}
