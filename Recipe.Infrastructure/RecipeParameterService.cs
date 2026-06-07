using EFCore.Infrastructure;
using EFCore.Repository;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    public class RecipeParameterService : EntityServiceBase<Recipe.Domain.RecipeParameter, Guid>, IRecipeParameterService
    {
        public RecipeParameterService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
