using System;
using System.Collections.Generic;
using System.Data;
using ProjectOrigin.Stamp.Server.Repositories;

namespace ProjectOrigin.Stamp.Server.Database;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private IRecipientRepository _recipientRepository = null!;

    public IRecipientRepository RecipientRepository
    {
        get
        {
            return _recipientRepository ??= new RecipientRepository(_lazyTransaction.Value.Connection ?? throw new InvalidOperationException("Transaction is null."));
        }
    }

    private ICertificateRepository _certificateRepository = null!;
    public ICertificateRepository CertificateRepository
    {
        get
        {
            return _certificateRepository ??= new CertificateRepository(_lazyTransaction.Value.Connection ?? throw new InvalidOperationException("Transaction is null."));
        }
    }

    private readonly Dictionary<Type, object> _repositories = new Dictionary<Type, object>();
    private readonly Lazy<IDbConnection> _lazyConnection;
    private Lazy<IDbTransaction> _lazyTransaction;
    private bool _disposed = false;

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _lazyConnection = new Lazy<IDbConnection>(() =>
        {
            var connection = connectionFactory.CreateConnection();
            connection.Open();
            return connection;
        });

        _lazyTransaction = new Lazy<IDbTransaction>(_lazyConnection.Value.BeginTransaction);
    }

    public void Commit()
    {
        if (!_lazyTransaction.IsValueCreated)
            return;

        try
        {
            _lazyTransaction.Value.Commit();
        }
        catch
        {
            _lazyTransaction.Value.Rollback();
            throw;
        }
        finally
        {
            ResetUnitOfWork();
        }
    }

    public void Rollback()
    {
        if (!_lazyTransaction.IsValueCreated)
            return;

        _lazyTransaction.Value.Rollback();

        ResetUnitOfWork();
    }


    public T GetRepository<T>(Func<IDbConnection, T> factory) where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var foundRepository))
        {
            return (T)foundRepository;
        }
        else
        {
            var newRepository = factory(_lazyTransaction.Value.Connection ?? throw new InvalidOperationException("Transaction is null."));
            _repositories.Add(typeof(T), newRepository);
            return newRepository;
        }
    }
    private void ResetUnitOfWork()
    {
        if (_lazyTransaction.IsValueCreated)
            _lazyTransaction.Value.Dispose();

        _lazyTransaction = new Lazy<IDbTransaction>(_lazyConnection.Value.BeginTransaction);

        _repositories.Clear();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~UnitOfWork() => Dispose(false);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;

            if (_lazyTransaction.IsValueCreated)
            {
                _lazyTransaction.Value.Dispose();
            }

            if (_lazyConnection.IsValueCreated)
            {
                _lazyConnection.Value.Dispose();
            }
        }

        _repositories.Clear();
    }
}
