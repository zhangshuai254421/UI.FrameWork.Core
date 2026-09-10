using Framework.Device;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Device.Domain
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDeviceServices(this IServiceCollection services)
        {
            services.AddScoped<IDeviceConfigurationReader, DeviceConfigurationReader>();
            return services;
        }
    }
}
