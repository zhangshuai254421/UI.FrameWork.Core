// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

using System.Linq.Expressions;

namespace EFCore.Repository
{
    /// <summary>
    /// 泛型仓储接口：约定对单一实体类型 <typeparamref name="TEntity"/> 的增、删、改、查与分页操作。
    /// 只负责对上下文施加变更，不负责提交——持久化时机由 <see cref="IUnitOfWork.SaveChangeAsync"/> 决定。
    /// <para>实现约定：<c>GetListAsync</c> 系列查询为无跟踪（AsNoTracking），返回脱离上下文的实体；
    /// 需要跟踪实体时用 <see cref="GetQueryable"/>（默认跟踪）自行组合查询。</para>
    /// </summary>
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> GetQueryable(bool isTracking = true);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        bool Delete(TEntity entity);

        void DeleteRange(IEnumerable<TEntity> entities);

        void DeleteRange(Expression<Func<TEntity, bool>> predicate);

        Task<bool> DeleteAsync(object[] keys, CancellationToken cancellationToken = default);

        Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        bool Update(TEntity entity);

        void UpdateRange(IEnumerable<TEntity> entities);

        void UpdateRange(Expression<Func<TEntity, bool>> predicate, Action<TEntity> updateAction);

        Task<TEntity?> FindAsync(object[] keys, CancellationToken cancellationToken = default);

        Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(PageParameter parameter, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(int pageNum, int pageSize, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(
             Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, PageParameter parameter, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, int pageNum, int pageSize, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
