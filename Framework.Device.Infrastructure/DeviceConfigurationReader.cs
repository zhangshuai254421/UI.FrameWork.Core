using Framework.Device.Domain;
using Framework.Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Framework.Device.Infrastructure
{
    /// <summary>从设备数据库读取设备配置并映射为契约对象，供设备管理器启动时使用。</summary>
    public class DeviceConfigurationReader : IDeviceConfigurationReader
    {
        /// <summary>设备数据库上下文（由依赖注入提供）。</summary>
        private readonly DeviceDataContext _context;

        /// <summary>构造：注入设备数据库上下文。</summary>
        public DeviceConfigurationReader(DeviceDataContext context)
        {
            _context = context;
        }

        /// <summary>读取全部设备配置：按 Id 升序、不跟踪实体，逐条映射为契约配置对象。</summary>
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
