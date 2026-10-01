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
        /// <summary>获取实体查询源：默认跟踪，传 false 为无跟踪；供自行组合 LINQ 查询。</summary>
        IQueryable<TEntity> GetQueryable(bool isTracking = true);

        /// <summary>把实体标记为新增，只施加变更不提交。</summary>
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>把一批实体标记为新增，只施加变更不提交。</summary>
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>把跟踪实体标记为删除；返回该实体是否进入删除状态。</summary>
        bool Delete(TEntity entity);

        /// <summary>把一批实体标记为删除，只施加变更不提交。</summary>
        void DeleteRange(IEnumerable<TEntity> entities);

        /// <summary>先按条件查出实体再全部标记为删除，只施加变更不提交。</summary>
        void DeleteRange(Expression<Func<TEntity, bool>> predicate);

        /// <summary>按主键数组查找并标记删除；实体不存在时不做变更并返回 false。</summary>
        Task<bool> DeleteAsync(object[] keys, CancellationToken cancellationToken = default);

        /// <summary>按条件在数据库端直接批量删除（绕过变更跟踪、立即生效）；返回删除行数。</summary>
        Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>把实体整体标记为已修改；返回该实体是否进入修改状态。</summary>
        bool Update(TEntity entity);

        /// <summary>清空变更跟踪后把一批实体标记为已修改。</summary>
        void UpdateRange(IEnumerable<TEntity> entities);

        /// <summary>按条件查出实体并对每条执行更新委托，只施加变更不提交。</summary>
        void UpdateRange(Expression<Func<TEntity, bool>> predicate, Action<TEntity> updateAction);

        /// <summary>按主键数组查找：优先命中变更跟踪缓存，未命中再查库；不存在返回 null。</summary>
        Task<TEntity?> FindAsync(object[] keys, CancellationToken cancellationToken = default);

        /// <summary>按条件无跟踪查找第一条；不存在返回 null。</summary>
        Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>无跟踪查询全部实体；无数据返回空序列。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

        /// <summary>无跟踪分页查询全部实体；页码从 1 开始。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(PageParameter parameter, CancellationToken cancellationToken = default);

        /// <summary>无跟踪分页查询全部实体；页码从 1 开始。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(int pageNum, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>无跟踪按条件查询实体列表。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(
             Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>无跟踪按条件分页查询；页码从 1 开始。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, PageParameter parameter, CancellationToken cancellationToken = default);

        /// <summary>无跟踪按条件分页查询；页码从 1 开始。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, int pageNum, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>统计全部实体条数。</summary>
        Task<int> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>统计符合条件的实体条数。</summary>
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>判断是否存在符合条件的记录。</summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
