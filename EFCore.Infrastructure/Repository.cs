using EFCore.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace EFCore.Infrastructure
{
    internal class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DataContext _context;

        public Repository(DataContext context)
        {
            _context = context;
        }

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

        public virtual Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return _context.AddAsync(entity, cancellationToken).AsTask();
        }

        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            return _context.AddRangeAsync(entities, cancellationToken);
        }

        public virtual bool Delete(TEntity entity)
        {
            var entry = _context.Remove(entity);

            return entry.State == EntityState.Deleted;
        }

        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {
            _context.RemoveRange(entities);
        }

        public virtual void DeleteRange(Expression<Func<TEntity, bool>> predicate)
        {
            var queryable = this.GetQueryable();

            var entities = queryable.Where(predicate);

            _context.RemoveRange(entities);
        }

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

        public virtual Task<int> ExecuteDeleteAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable();

            var entities = queryable.Where(predicate);

            return entities.ExecuteDeleteAsync(cancellationToken);
        }

        public virtual bool Update(TEntity entity)
        {
            var entry = _context.Update(entity);

            return entry.State == EntityState.Modified;
        }

        public virtual void UpdateRange(IEnumerable<TEntity> entities)
        {
            _context.UpdateRange(entities);
        }

        public virtual void UpdateRange(Expression<Func<TEntity, bool>> predicate, Action<TEntity> updateAction)
        {
            var queryable = this.GetQueryable();

            var entities = queryable.Where(predicate);

            foreach (var entity in entities)
            {
                updateAction(entity);
            }
        }

        public virtual Task<TEntity?> FindAsync(object[] keys, CancellationToken cancellationToken = default)
        {
            return _context.Set<TEntity>().FindAsync(keys, cancellationToken).AsTask();
        }

        public virtual Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return queryable.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable.ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetListAsync(
            PageParameter parameter, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Skip((parameter.PageNum - 1) * parameter.PageSize)
                .Take(parameter.PageSize)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetListAsync(
            int pageNum, int pageSize, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return await queryable
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

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

        public virtual Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return queryable.CountAsync(cancellationToken);
        }

        public virtual Task<int> CountAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var queryable = this.GetQueryable(false);

            return queryable.CountAsync(predicate, cancellationToken);
        }

        public Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = this.GetQueryable(false);

            return query.AnyAsync(predicate, cancellationToken);
        }
    }
}
