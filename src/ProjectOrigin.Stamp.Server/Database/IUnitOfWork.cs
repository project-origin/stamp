using ProjectOrigin.Stamp.Server.Repositories;

namespace ProjectOrigin.Stamp.Server.Database;

public interface IUnitOfWork
{
    void Commit();
    void Rollback();

    IRecipientRepository RecipientRepository { get; }
}
