using EFCore.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Infrastructure
{
    /// <summary>
    /// IUnitOfWork 的 EF Core 实现：从容器解析各实体的仓储，并统一提交变更或开启事务。
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        /// <summary>最近一次获取仓储时解析出的上下文，供事务与提交使用。</summary>
        private  DbContext _context;
        /// <summary>服务提供程序：用于解析各实体类型的仓储。</summary>
        private readonly IServiceProvider _provider;

        /// <summary>保存服务提供程序；上下文延迟到首次获取仓储时解析。</summary>
        public UnitOfWork( IServiceProvider provider)
        {
          
            _provider = provider;
        }

        /// <inheritdoc/>
        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            var temp = _provider.GetRequiredService<IRepository<TEntity>>();
            _context = ((Repository<TEntity>)temp)._context;
            return temp;
        }

        /// <inheritdoc/>
        public async Task<IUnitOfTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            return new UnitOfTransaction(transaction);
        }

        /// <inheritdoc/>
        public Task<int> SaveChangeAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
