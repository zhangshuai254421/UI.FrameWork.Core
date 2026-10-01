using EFCore.Repository;
using Framework.Core.CustomAttribute;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace EFCore.Infrastructure
{
    /// <summary>
    /// 基于 EF Core 的泛型仓储实现：按实体类型解析对应的 DbContext，只施加变更不提交；
    /// GetListAsync 系列查询无跟踪，FindAsync 优先命中变更跟踪缓存。
    /// </summary>
    internal class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        /// <summary>当前实体类型对应的 EF Core 上下文。</summary>
        internal readonly DbContext _context;

        /// <summary>通过解析器为当前实体类型解析出 DbContext。</summary>
        public Repository(IDbContextResolver resolver)
        {
            _context = resolver.Resolve<TEntity>();;
        }

        /// <inheritdoc/>
        public IQueryable<TEntity> GetQueryable(bool isTracking = true)
        {
            if (isTracking)
            {
                return _context.Set<TEntity>();
            }
            else
            {
                return _context.Set<TEntity>().AsNoTracking();
            }
        }

        /// <inheritdoc/>
        public virtual Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return _context.AddAsync(entity, cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            return _context.AddRangeAsync(entities, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual bool Delete(TEntity entity)
        {
            var entry = _context.Remove(entity);

            return entry.State == EntityState.Deleted;
        }

        /// <inheritdoc/>
        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {
            _context.RemoveRange(entities);
        }

        /// <inheritdoc/>
        public virtual void DeleteRange(Expression<Func<TEntity, bool>> predicate)
        {
            var queryable = this.GetQueryable();

            var entities = queryable.Where(predicate);

            _context.RemoveRange(entities);
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(object[] keys, CancellationToken cancellationToken = default)
        {
            var entity = await this.FindAsync(keys, cancellationToken);

            if (entity != null)
            {
                _context.Remove(entity);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual Task<int> ExecuteDeleteAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable();

            var entities = queryable.Where(predicate);

            return entities.ExecuteDeleteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual bool Update(TEntity entity)
        {
            var entry = _context.Update(entity);

            return entry.State == EntityState.Modified;
        }

        /// <inheritdoc/>
        public virtual void UpdateRange(IEnumerable<TEntity> entities)
        {
            _context.ChangeTracker.Clear();
            _context.UpdateRange(entities);
        }

        /// <inheritdoc/>
        public virtual void UpdateRange(Expression<Func<TEntity, bool>> predicate, Action<TEntity> updateAction)
        {
            var queryable = this.GetQueryable();

            var entities = queryable.Where(predicate);

            foreach (var entity in entities)
            {
                updateAction(entity);
            }
        }

        /// <inheritdoc/>
        public virtual Task<TEntity?> FindAsync(object[] keys, CancellationToken cancellationToken = default)
        {
            return _context.Set<TEntity>().FindAsync(keys, cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        public virtual Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return queryable.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable.ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> GetListAsync(
            PageParameter parameter, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Skip((parameter.PageNum - 1) * parameter.PageSize)
                .Take(parameter.PageSize)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> GetListAsync(
            int pageNum, int pageSize, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, PageParameter parameter, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Where(predicate)
                .Skip((parameter.PageNum - 1) * parameter.PageSize)
                .Take(parameter.PageSize)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, int pageNum, int pageSize, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Where(predicate)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return queryable.CountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<int> CountAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return queryable.CountAsync(predicate, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = this.GetQueryable(false);

            return query.AnyAsync(predicate, cancellationToken);
        }
    }
}
