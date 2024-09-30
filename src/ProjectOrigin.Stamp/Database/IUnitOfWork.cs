using ProjectOrigin.Stamp.Repositories;

namespace ProjectOrigin.Stamp.Database;

public interface IUnitOfWork
{
    void Commit();
    void Rollback();

    IRecipientRepository RecipientRepository { get; }
    ICertificateRepository CertificateRepository { get; }
    IOutboxMessageRepository OutboxMessageRepository { get; }
    IWithdrawnCertificateRepository WithdrawnCertificateRepository { get; }
}
