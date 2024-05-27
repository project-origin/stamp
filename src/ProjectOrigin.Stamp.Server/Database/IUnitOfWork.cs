using ProjectOrigin.Stamp.Server.Repositories;

namespace ProjectOrigin.Stamp.Server.Database;

public interface IUnitOfWork
{
    void Commit();
    void Rollback();

    IRecipientRepository RecipientRepository { get; }
    ICertificateRepository CertificateRepository { get; }
    IOutboxMessageRepository OutboxMessageRepository { get; }
}
