using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Framework.Device
{
    /// <summary>
    /// 设备管理器：读配置 → 用 (设备类型, 厂商) 解析适配器 → 实例化 → 按连接方式打开 → 统一维护生命周期。
    /// 单台设备打开失败时降级运行（记录错误但不拖垮整机）；退出时统一关闭所有已打开设备。
    /// </summary>
    public class DeviceManager : IDisposable
    {
        /// <summary>适配器工厂：按 (设备类型, 厂商) 实例化适配器。</summary>
        private readonly IDeviceAdapterFactory _factory;
        private readonly ILogger<DeviceManager> _logger;

        /// <summary>已打开设备表：配置 Id → 实例。</summary>
        private readonly Dictionary<string, IDevice> _devices = new();

        /// <summary>构造：注入适配器工厂与可选日志。</summary>
        public DeviceManager(IDeviceAdapterFactory factory, ILogger<DeviceManager>? logger = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? NullLogger<DeviceManager>.Instance;
        }

        /// <summary>按配置逐个解析并打开设备；单台失败不影响其余。</summary>
        public void Initialize(IEnumerable<DeviceConfiguration> configurations)
        {
            foreach (var config in configurations)
            {
                OpenDevice(config);
            }
        }

        /// <summary>从配置读取器（生产实现为数据库）读取配置并初始化。</summary>
        public void Initialize(IDeviceConfigurationReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            Initialize(reader.Read());
        }

        /// <summary>通过设备标识取得已初始化的设备（而非具体类型）。未找到返回 null。</summary>
        public IDevice? GetDevice(string id)
        {
            return _devices.TryGetValue(id, out var device) ? device : null;
        }

        /// <summary>统一关闭所有已打开设备。</summary>
        public void CloseAll()
        {
            foreach (var device in _devices.Values)
            {
                device.Close();
            }

            _devices.Clear();
        }

        /// <summary>释放并关闭所有已打开设备。</summary>
        public void Dispose() => CloseAll();

        private void OpenDevice(DeviceConfiguration config)
        {
            try
            {
                var device = _factory.Create(config.Kind, config.Vendor);
                if (device.Open(config.Connection))
                {
                    if (_devices.TryGetValue(config.Id, out var existing))
                    {
                        _logger.LogWarning("设备 {Id} 已存在，关闭旧实例并替换", config.Id);
                        existing.Close();
                    }

                    _devices[config.Id] = device;
                }
                else
                {
                    _logger.LogError(
                        "设备 {Id}（{Kind}/{Vendor}）打开失败，已降级跳过",
                        config.Id, config.Kind, config.Vendor);
                    device.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "设备 {Id}（{Kind}/{Vendor}）初始化失败，已降级跳过",
                    config.Id, config.Kind, config.Vendor);
            }
        }
    }
}
