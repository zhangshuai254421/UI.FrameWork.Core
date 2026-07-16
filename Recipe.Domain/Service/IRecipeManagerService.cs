using EFCore.IRepository;

namespace Recipe.Domain
{
    public interface IRecipeManagerService : IEntityServiceBase<RecipeManager, Guid>
    {

    }
}
