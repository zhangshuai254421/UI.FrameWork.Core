using EFCore.Infrastructure;
using EFCore.Repository;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    public class RecipeService : EntityServiceBase<Recipe.Domain.Recipe, Guid>, IRecipeService
    {
        public RecipeService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }

    public class RecipeManagerService : EntityServiceBase<Recipe.Domain.RecipeManager, Guid>, IRecipeManagerService
    {
        public RecipeManagerService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
