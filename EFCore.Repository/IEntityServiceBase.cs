using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.IRepository
{
    public interface IEntityServiceBase<TEntity, TKey> where TEntity : class
    {
        Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> AddRangeAsync(
            IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(TKey key, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> UpdateRangeAsync(
            IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        Task<TEntity?> GetAsync(TKey key, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

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
