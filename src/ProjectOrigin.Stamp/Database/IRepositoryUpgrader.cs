using System.Threading.Tasks;

namespace ProjectOrigin.Stamp.Database;

public interface IRepositoryUpgrader
{
    Task Upgrade();
    Task<bool> IsUpgradeRequired();
}
