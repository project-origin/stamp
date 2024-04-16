using System.Threading.Tasks;

namespace ProjectOrigin.Stamp.Server.Database;

public interface IRepositoryUpgrader
{
    Task Upgrade();
    Task<bool> IsUpgradeRequired();
}
