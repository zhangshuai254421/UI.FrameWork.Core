using EFCore.IRepository;
using EFCore.Repository;
using Framework.Core.Common;

namespace Recipe.Domain
{

    /// <summary>
    /// 定义一个泛型接口 IRecipeParameterServiceBase，继承自 IEntityServiceBase<TEntity, TKey>，用于处理配方参数相关的服务操作。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRecipeParameterServiceBase<TEntity, TKey> : IEntityServiceBase<TEntity, TKey>
    where TEntity : class
    {

       Task<IEnumerable<TEntity>> GetCurrentRecipeParameterAsync(CancellationToken cancellationToken = default);

        // 新增使用泛型的方法，比如根据 TKey 获取实体
    }

    /// <summary>
    /// 定义一个接口 IRecipeParameterService，继承自 IRecipeParameterServiceBase<RecipeParameter, Guid>，用于处理配方参数相关的服务操作。
    /// </summary>
    public interface IRecipeParameterService :  IRecipeParameterServiceBase<RecipeParameter, Guid>
    {
       
    }
}
