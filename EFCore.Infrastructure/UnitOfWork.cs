using EFCore.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private  DbContext _context;
        private readonly IServiceProvider _provider;

        public UnitOfWork( IServiceProvider provider)
        {
          
            _provider = provider;
        }

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            var temp = _provider.GetRequiredService<IRepository<TEntity>>();
            _context = ((Repository<TEntity>)temp)._context;
            return temp;
        }

        public async Task<IUnitOfTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            return new UnitOfTransaction(transaction);
        }

        public Task<int> SaveChangeAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
