using EFCore.IRepository;

namespace Recipe.Domain
{
    /// <summary>
    /// 配方管理器服务接口：提供配方管理器实体（记录当前选中配方）的通用增删改查能力。
    /// </summary>
    public interface IRecipeManagerService : IEntityServiceBase<RecipeManager, Guid>
    {

    }
}
