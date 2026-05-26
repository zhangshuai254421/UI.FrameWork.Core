namespace EFCore.Repository
{
    public interface IUnitOfWork
    {
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
        Task<int> SaveChangeAsync(CancellationToken cancellationToken = default);
        Task<IUnitOfTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
