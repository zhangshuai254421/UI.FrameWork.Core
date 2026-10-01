using EFCore.IRepository;
using EFCore.Repository;
using Recipe.Domain;
using System.Linq.Expressions;

namespace Recipe.Infrastructure
{

    /// <summary>
    /// 配方参数服务：面向配方参数基类的通用增删改查与当前配方参数查询。
    /// </summary>
    public class RecipeParameterService : RecipeParameterServiceBase<Recipe.Domain.RecipeParameter, Guid>,IRecipeParameterService
    {
        /// <summary>构造函数：注入工作单元与配方管理器服务。</summary>
        public RecipeParameterService(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService) : base(unitofWork, recipeManagerService)
        {
        }

  
    }
}
