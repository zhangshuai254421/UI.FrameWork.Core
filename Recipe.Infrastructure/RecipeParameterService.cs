using EFCore.IRepository;
using EFCore.Repository;
using Recipe.Domain;
using System.Linq.Expressions;

namespace Recipe.Infrastructure
{

    public class RecipeParameterService : RecipeParameterServiceBase<Recipe.Domain.RecipeParameter, Guid>,IRecipeParameterService
    {
        public RecipeParameterService(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService) : base(unitofWork, recipeManagerService)
        {
        }

  
    }
}
