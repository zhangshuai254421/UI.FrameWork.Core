
using EFCore.Infrastructure;
using EFCore.IRepository;
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
            services.AddScoped<IRecipeManagerService, RecipeManagerService>();

            // 插件实体走开放泛型兜底；内置实体仍命中下面的闭合注册，互不冲突
            services.AddScoped(typeof(IRecipeParameterServiceBase<,>), typeof(GenericRecipeParameterService<,>));
            services.AddScoped(typeof(IEntityServiceBase<,>), typeof(GenericSystemParameterService<,>));
            return services;
        }
    }
}
