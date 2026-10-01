using Framework.Device.Domain;
using Framework.Device.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace Framework.Device.Infrastructure
{
    /// <summary>从设备数据库读取设备配置并映射为契约对象，供设备管理器启动时使用。</summary>
    public class DeviceConfigurationReader : IDeviceConfigurationReader
    {
        private readonly DeviceDataContext _context;

        public DeviceConfigurationReader(DeviceDataContext context)
        {
            _context = context;
        }

        public IReadOnlyList<DeviceConfiguration> Read()
        {
            return _context.DeviceConfigurations
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .Select(x => x.ToDeviceConfiguration())
                .ToList();
        }
    }
}
