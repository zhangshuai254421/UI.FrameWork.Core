// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
namespace EFCore.IRepository
{
    /// <summary>
    /// 实体服务通用接口：面向业务层约定增、删、改、查与分页操作。与
    /// <see cref="IRepository{TEntity}"/> 只改不提交不同，本接口的方法自带提交语义：
    /// 调用返回时变更已写入数据库，true/false 表示是否实际影响了数据行。
    /// <typeparamref name="TEntity"/> 为实体类型，<typeparamref name="TKey"/> 为主键类型。
    /// </summary>
    public interface IEntityServiceBase<TEntity, TKey> where TEntity : class
    {
        Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> AddRangeAsync(
            IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(TKey key, CancellationToken cancellationToken = default);

        Task<bool> DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate,CancellationToken cancellationToken = default);

        /// <summary>
        /// 要传入跟踪实体  才能更新成功
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> UpdateRangeAsync(
            IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task<bool> UpdateRangeAsync
    (Expression<Func<TEntity, bool>> predicate, Action<TEntity> updateAction, CancellationToken cancellationToken = default);



        Task<TEntity?> GetAsync(TKey key, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(
     Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<PagedResult<TEntity>> GetPageAsync(
            PageParameter parameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// 带条件过滤的分页查询
        /// </summary>
        Task<PagedResult<TEntity>> GetPageAsync(
            Expression<Func<TEntity, bool>> predicate,
            PageParameter parameter,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
