using EFCore.Repository;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.Infrastructure
{
    internal class UnitOfTransaction : IUnitOfTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public UnitOfTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public bool SupportsSavepoints => _transaction.SupportsSavepoints;

        public async Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (this.SupportsSavepoints)
            {
                await _transaction.CreateSavepointAsync(name, cancellationToken);
            }

            throw new NotSupportedException();
        }

        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (this.SupportsSavepoints)
            {
                await _transaction.RollbackToSavepointAsync(name, cancellationToken);
            }

            throw new NotSupportedException();
        }

        public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (this.SupportsSavepoints)
            {
                await _transaction.ReleaseSavepointAsync(name, cancellationToken);
            }

            throw new NotSupportedException();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }
}
