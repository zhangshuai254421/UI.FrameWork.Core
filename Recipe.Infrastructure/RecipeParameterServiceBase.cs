using EFCore.Infrastructure;
using EFCore.Repository;
using Framework.Core.Common;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    public abstract class RecipeParameterServiceBase<TEntity, TKey>
    : EntityServiceBase<TEntity, TKey>, IRecipeParameterServiceBase<TEntity, TKey>
    where TEntity : RecipeParameter
    where TKey : notnull
    {
        protected RecipeParameterServiceBase(IUnitOfWork unitofWork) : base(unitofWork)
        {
            
        }

        public virtual Task<IEnumerable<TEntity>> GetCurrentRecipeParameterAsync(CancellationToken cancellationToken = default)
        {
            int crrentRecipeId = IoC.Get<IRecipeManagerService>().GetListAsync().Result.FirstOrDefault().CurrentRecipeId;
            return _repository.GetListAsync(p => p.RecipeId == crrentRecipeId,cancellationToken);
        }
    }

    public class GenericRecipeParameterService<TEntity, TKey>
      : RecipeParameterServiceBase<TEntity, TKey>
      where TEntity : RecipeParameter
      where TKey : notnull
    {
        public GenericRecipeParameterService(IUnitOfWork unitofWork) : base(unitofWork) { }
    }

    public class GenericSystemParameterService<TEntity, TKey>
        : EntityServiceBase<TEntity, TKey>
          where TEntity : Entity
      where TKey : notnull
    {
        public GenericSystemParameterService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
