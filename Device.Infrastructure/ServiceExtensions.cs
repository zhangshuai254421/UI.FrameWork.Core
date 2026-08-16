using Device.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Infrastructure
{
    /// <summary>
    /// 设备模块 DI 注册扩展
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// 注册设备相关服务到 DI 容器
        /// </summary>
        public static IServiceCollection AddDeviceServices(this IServiceCollection services)
        {
            // 设备配置 CRUD（Scoped，遵循 EF Core DbContext 生命周期）
            services.AddScoped<IDeviceConfigurationService, DeviceConfigurationService>();

            // 插件加载器（Singleton，内部无状态）
            services.AddSingleton<DevicePluginLoader>();

            // 设备管理器（Singleton，设备连接全应用共享）
            services.AddSingleton<IDeviceManager, DeviceManager>();

            return services;
        }
    }
}
