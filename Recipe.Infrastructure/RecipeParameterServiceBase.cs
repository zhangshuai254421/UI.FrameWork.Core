using EFCore.Infrastructure;
using EFCore.Repository;
using Framework.Core.Common;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 配方参数服务基类：在通用实体服务之上提供"按当前配方"查询参数的能力。
    /// </summary>
    /// <typeparam name="TEntity">配方参数实体类型。</typeparam>
    /// <typeparam name="TKey">主键类型。</typeparam>
    public abstract class RecipeParameterServiceBase<TEntity, TKey>
    : EntityServiceBase<TEntity, TKey>, IRecipeParameterServiceBase<TEntity, TKey>
    where TEntity : RecipeParameter
    where TKey : notnull
    {
        /// <summary>配方管理器服务，用于解析当前配方 Id。</summary>
        protected readonly IRecipeManagerService _recipeManagerService;

        /// <summary>构造函数：注入工作单元与配方管理器服务。</summary>
        protected RecipeParameterServiceBase(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService)
        : base(unitofWork)
        {
            _recipeManagerService = recipeManagerService;
        }

        /// <summary>获取当前配方下的全部参数；无可用配方管理器记录时抛异常。</summary>
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

    /// <summary>
    /// 配方参数开放泛型服务：为插件派生的参数实体提供兜底 DI 注册。
    /// </summary>
    public class GenericRecipeParameterService<TEntity, TKey>
      : RecipeParameterServiceBase<TEntity, TKey>
      where TEntity : RecipeParameter
      where TKey : notnull
    {
        /// <summary>构造函数：注入工作单元与配方管理器服务。</summary>
        public GenericRecipeParameterService(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService)
        : base(unitofWork, recipeManagerService) { }
    }

    /// <summary>
    /// 系统参数开放泛型服务：为插件派生的普通实体提供兜底 DI 注册。
    /// </summary>
    public class GenericSystemParameterService<TEntity, TKey>
        : EntityServiceBase<TEntity, TKey>
          where TEntity : Entity
      where TKey : notnull
    {
        /// <summary>构造函数：注入工作单元。</summary>
        public GenericSystemParameterService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
