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
}
