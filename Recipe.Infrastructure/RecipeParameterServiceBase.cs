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
        protected readonly IRecipeManagerService _recipeManagerService;

        protected RecipeParameterServiceBase(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService)
        : base(unitofWork)
        {
            _recipeManagerService = recipeManagerService;
        }

        public virtual async Task<IEnumerable<TEntity>> GetCurrentRecipeParameterAsync(CancellationToken cancellationToken = default)
        {
            int currentRecipeId = await GetCurrentRecipeIdAsync(cancellationToken);

            return await _repository.GetListAsync(p => p.RecipeId == currentRecipeId, cancellationToken);
        }

        /// <summary>
        /// 解析当前配方 Id：RecipeManager 表的唯一记录持有 <see cref="RecipeManager.CurrentRecipeId"/>，
        /// 各类"按当前配方查询"的参数都挂在这个 Id 下。
        /// </summary>
        protected async Task<int> GetCurrentRecipeIdAsync(CancellationToken cancellationToken = default)
        {
            var managers = await _recipeManagerService.GetListAsync(cancellationToken);

            var manager = managers.FirstOrDefault()
                ?? throw new InvalidOperationException("RecipeManager 表为空：尚未创建配方管理器记录，无法解析当前配方。");

            return manager.CurrentRecipeId;
        }
    }

    public class GenericRecipeParameterService<TEntity, TKey>
      : RecipeParameterServiceBase<TEntity, TKey>
      where TEntity : RecipeParameter
      where TKey : notnull
    {
        public GenericRecipeParameterService(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService)
        : base(unitofWork, recipeManagerService) { }
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
