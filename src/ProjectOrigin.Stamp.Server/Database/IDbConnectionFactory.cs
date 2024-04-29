using System.Data;

namespace ProjectOrigin.Stamp.Server.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
