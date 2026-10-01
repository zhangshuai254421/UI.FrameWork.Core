using EFCore.IRepository;
using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.Infrastructure
{
    /// <summary>
    /// 实体服务基类：组合工作单元与仓储，实现自带提交语义的通用增删改查与分页；各方法返回是否实际影响了数据行。
    /// </summary>
    public abstract class EntityServiceBase<TEntity, TKey>
       : IEntityServiceBase<TEntity, TKey>
       where TEntity : class
       where TKey : notnull
    {
        /// <summary>工作单元：负责把变更统一提交到数据库。</summary>
        protected readonly IUnitOfWork _unitOfWork;
        /// <summary>当前实体类型的仓储：负责施加增删改查变更。</summary>
        protected readonly IRepository<TEntity> _repository;

        /// <summary>从工作单元解析出当前实体类型的仓储。</summary>
        public EntityServiceBase(IUnitOfWork unitofWork)
        {
            _unitOfWork = unitofWork;
            _repository = _unitOfWork.GetRepository<TEntity>();
        }

        /// <inheritdoc/>
        public virtual async Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _repository.AddAsync(entity, cancellationToken);

            var affected = await _unitOfWork.SaveChangeAsync(cancellationToken);

            return affected > 0;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> AddRangeAsync(
            IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            await _repository.AddRangeAsync(entities, cancellationToken);

            var affected = await _unitOfWork.SaveChangeAsync(cancellationToken);

            return affected > 0;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            bool result;

            if (key is object[] keys)
            {
                result = await _repository.DeleteAsync(keys, cancellationToken);
            }
            else
            {
                result = await _repository.DeleteAsync(new object[] { key }, cancellationToken);
            }

            if (result)
            {
                var affected = await _unitOfWork.SaveChangeAsync(cancellationToken);

                return affected > 0;
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
             _repository.DeleteRange(predicate); 
             var affected = await _unitOfWork.SaveChangeAsync(cancellationToken);
             return affected > 0;
      
        }

        /// <inheritdoc/>
        public virtual async Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _repository.Update(entity);

            var affected = await _unitOfWork.SaveChangeAsync(cancellationToken);

            return affected > 0;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                _repository.UpdateRange(entities);

                var affected = await _unitOfWork.SaveChangeAsync(cancellationToken);

                return affected > 0;
            }
            catch (Exception e)
            {
              throw e;
            }

        }

        /// <inheritdoc/>
        public virtual async Task<bool> UpdateRangeAsync(Expression<Func<TEntity, bool>> predicate, Action<TEntity> updateAction, CancellationToken cancellationToken = default)
        {
            _repository.UpdateRange(predicate, updateAction);

            var affected = await _unitOfWork.SaveChangeAsync(cancellationToken);

            return affected > 0;
        }

        /// <inheritdoc/>
        public virtual Task<TEntity?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (key is object[] keys)
            {
                return _repository.FindAsync(keys, cancellationToken);
            }
            else
            {
                return _repository.FindAsync(new object[] { key }, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public virtual Task<IEnumerable<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
        {
            return _repository.GetListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<PagedResult<TEntity>> GetPageAsync(
            PageParameter parameter, CancellationToken cancellationToken = default)
        {
            var result = new PagedResult<TEntity>();

            result.Total = await _repository.CountAsync(cancellationToken);

            result.Data = await _repository.GetListAsync(parameter, cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<PagedResult<TEntity>> GetPageAsync(
            Expression<Func<TEntity, bool>> predicate,
            PageParameter parameter,
            CancellationToken cancellationToken = default)
        {
            var result = new PagedResult<TEntity>();

            result.Total = await _repository.CountAsync(predicate, cancellationToken);

            result.Data = await _repository.GetListAsync(predicate, parameter, cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public virtual Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return _repository.CountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
           return _repository.GetListAsync(predicate, cancellationToken);
        }
    }
}
