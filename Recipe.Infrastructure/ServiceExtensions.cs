
using EFCore.Infrastructure;
using EFCore.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Recipe.Domain;
using Recipe.Infrastructure.Contracts;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 配方模块依赖注入扩展。
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>注册配方模块服务：配方、参数、相机配置、管理器四个服务，以及插件实体的开放泛型兜底服务。</summary>
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
