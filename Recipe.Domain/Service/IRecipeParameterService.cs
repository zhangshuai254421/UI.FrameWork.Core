using EFCore.IRepository;
using EFCore.Repository;
using Framework.Core.Common;

namespace Recipe.Domain
{


    public interface IRecipeParameterServiceBase<TEntity, TKey> : IEntityServiceBase<TEntity, TKey>
    where TEntity : class
    {

       Task<IEnumerable<TEntity>> GetCurrentRecipeParameterAsync(CancellationToken cancellationToken = default);

        // 新增使用泛型的方法，比如根据 TKey 获取实体
    }
    public interface IRecipeParameterService :  IRecipeParameterServiceBase<RecipeParameter, Guid>
    {
       
    }
}
