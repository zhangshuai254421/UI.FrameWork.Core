using EFCore.Infrastructure;
using EFCore.Repository;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 配方管理器服务：提供配方管理器实体（记录当前选中配方）的通用增删改查。
    /// </summary>
    public class RecipeManagerService : EntityServiceBase<Recipe.Domain.RecipeManager, Guid>, IRecipeManagerService
    {
        /// <summary>构造函数：注入工作单元。</summary>
        public RecipeManagerService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
