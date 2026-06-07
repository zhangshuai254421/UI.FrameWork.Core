
using Microsoft.Extensions.DependencyInjection;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRecipeServices(this IServiceCollection services)
        {
            services.AddScoped<IRecipeService, RecipeService>();
            services.AddScoped<IRecipeParameterService, RecipeParameterService>();
            services.AddScoped<ICameraConfigurationService, CameraConfigurationService>();
            return services;
        }
    }
}
