using System.Data;

namespace ProjectOrigin.Stamp.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
