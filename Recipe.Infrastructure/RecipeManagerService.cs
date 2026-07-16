using EFCore.Infrastructure;
using EFCore.Repository;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    public class RecipeManagerService : EntityServiceBase<Recipe.Domain.RecipeManager, Guid>, IRecipeManagerService
    {
        public RecipeManagerService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
