using EFCore.Repository;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.Infrastructure
{
    /// <summary>
    /// IUnitOfTransaction 的 EF Core 实现：包装 DbContextTransaction，提供提交、回滚与保存点能力。
    /// </summary>
    internal class UnitOfTransaction : IUnitOfTransaction
    {
        /// <summary>底层 EF Core 数据库事务。</summary>
        private readonly IDbContextTransaction _transaction;

        /// <summary>包装已开启的数据库事务。</summary>
        public UnitOfTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        /// <inheritdoc/>
        public bool SupportsSavepoints => _transaction.SupportsSavepoints;

        /// <inheritdoc/>
        public async Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (this.SupportsSavepoints)
            {
                await _transaction.CreateSavepointAsync(name, cancellationToken);
            }

            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (this.SupportsSavepoints)
            {
                await _transaction.RollbackToSavepointAsync(name, cancellationToken);
            }

            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (this.SupportsSavepoints)
            {
                await _transaction.ReleaseSavepointAsync(name, cancellationToken);
            }

            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _transaction.Dispose();
        }
    }
}
