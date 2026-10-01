using Framework.Device;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Framework.Device.Infrastructure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDeviceServices(this IServiceCollection services)
        {
            services.AddScoped<IDeviceConfigurationReader, DeviceConfigurationReader>();

            // 适配器工厂：启动时按配置目录动态加载厂商适配器 DLL，不硬引用具体厂商。
            services.AddSingleton<IDeviceAdapterFactory>(_ =>
                DeviceAdapterFactory.FromDirectory(AppDomain.CurrentDomain.BaseDirectory));

            // 设备管理器：单例，持有整机已打开设备，退出时统一关闭。
            services.AddSingleton(sp => new DeviceManager(
                sp.GetRequiredService<IDeviceAdapterFactory>(),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DeviceManager>()));

            return services;
        }
    }
}
