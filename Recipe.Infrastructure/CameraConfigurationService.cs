using EFCore.Infrastructure;
using EFCore.Repository;
using Recipe.Domain;

namespace Recipe.Infrastructure
{


    public class CameraConfigurationService : RecipeParameterServiceBase<Recipe.Domain.CameraConfiguration, Guid>, ICameraConfigurationService   
    {
        public CameraConfigurationService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
