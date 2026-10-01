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
        /// <summary>新增单条实体并立即提交；返回是否实际写入了数据。</summary>
        Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>批量新增实体并立即提交；返回是否实际写入了数据。</summary>
        Task<bool> AddRangeAsync(
            IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>按主键删除实体并立即提交；主键不存在时返回 false。</summary>
        Task<bool> DeleteAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>按条件批量删除并立即提交；返回是否实际删除了数据行。</summary>
        Task<bool> DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate,CancellationToken cancellationToken = default);

        /// <summary>
        /// 要传入跟踪实体  才能更新成功
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>批量更新传入的跟踪实体并立即提交；返回是否实际修改了数据。</summary>
        Task<bool> UpdateRangeAsync(
            IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        /// <summary>按条件查出实体、对每条执行更新委托后立即提交；返回是否有数据被修改。</summary>
        Task<bool> UpdateRangeAsync
    (Expression<Func<TEntity, bool>> predicate, Action<TEntity> updateAction, CancellationToken cancellationToken = default);



        /// <summary>按主键查询单条实体；不存在时返回 null。</summary>
        Task<TEntity?> GetAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>查询全部实体（无跟踪）；无数据时返回空序列。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

        /// <summary>按条件查询实体列表（无跟踪）；无匹配时返回空序列。</summary>
        Task<IEnumerable<TEntity>> GetListAsync(
     Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>无条件分页查询；返回总记录数与当前页数据。</summary>
        Task<PagedResult<TEntity>> GetPageAsync(
            PageParameter parameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// 带条件过滤的分页查询
        /// </summary>
        Task<PagedResult<TEntity>> GetPageAsync(
            Expression<Func<TEntity, bool>> predicate,
            PageParameter parameter,
            CancellationToken cancellationToken = default);

        /// <summary>统计全部记录数。</summary>
        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
