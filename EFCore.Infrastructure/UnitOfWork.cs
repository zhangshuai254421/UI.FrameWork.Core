using EFCore.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _context;
        private readonly IServiceProvider _provider;

        public UnitOfWork(DataContext context, IServiceProvider provider)
        {
            _context = context;
            _provider = provider;
        }

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            return _provider.GetRequiredService<IRepository<TEntity>>();
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
