using EFCore.IRepository;

namespace Recipe.Domain
{
    public interface IRecipeService : IEntityServiceBase<Recipe, Guid>
    {

    }

    public interface IRecipeManagerService : IEntityServiceBase<RecipeManager, Guid>
    {

    }
}
