using System.Collections.Concurrent;
using Device.Domain;
using FrameWork.Device;
using Microsoft.Extensions.Logging;

namespace Device.Infrastructure
{
    /// <summary>
    /// 设备管理器 - 管理所有硬件设备的完整生命周期
    /// Singleton 生命周期，全应用共享设备连接
    /// </summary>
    public class DeviceManager : IDeviceManager, IDisposable
    {
        #region 字段

        private readonly DevicePluginLoader _pluginLoader;
        private readonly ILogger<DeviceManager> _logger;
        private readonly ConcurrentDictionary<string, IDevice> _devices = new();
        private volatile bool _isInitialized;
        private bool _disposed;

        #endregion

        #region 构造函数

        public DeviceManager(
            DevicePluginLoader pluginLoader,
            ILogger<DeviceManager> logger)
        {
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 属性

        public bool IsInitialized => _isInitialized;

        #endregion

        #region IDeviceManager 实现

        public async Task InitializeAllAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                _logger.LogWarning("设备管理器已初始化，跳过重复初始化");
                return;
            }

            _logger.LogInformation("========== 设备管理器开始初始化 ==========");

            try
            {
                // 1. 加载所有设备插件
                var devices = await _pluginLoader.LoadDevicePluginsAsync(cancellationToken);

                // 2. 注册设备到字典
                foreach (var device in devices)
                {
                    if (_devices.TryAdd(device.DeviceCode, device))
                    {
                        _logger.LogDebug("设备 [{DeviceCode}] 已注册到管理器", device.DeviceCode);
                    }
                }

                // 3. 连接所有设备
                foreach (var device in devices)
                {
                    try
                    {
                        _logger.LogInformation("正在连接设备 [{DeviceCode}] {DeviceName}...",
                            device.DeviceCode, device.DeviceName);

                        var connected = await device.ConnectAsync();
                        if (connected)
                        {
                            _logger.LogInformation("设备 [{DeviceCode}] 连接成功", device.DeviceCode);
                        }
                        else
                        {
                            _logger.LogWarning("设备 [{DeviceCode}] 连接失败", device.DeviceCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "设备 [{DeviceCode}] 连接异常", device.DeviceCode);
                    }
                }

                _isInitialized = true;
                _logger.LogInformation("========== 设备管理器初始化完成，共 {Count} 个设备 ==========", _devices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备管理器初始化失败");
                throw;
            }
        }

        public async Task ShutdownAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("========== 设备管理器开始关闭 ==========");

            foreach (var kvp in _devices)
            {
                try
                {
                    _logger.LogInformation("正在断开设备 [{DeviceCode}]...", kvp.Key);
                    await kvp.Value.DisconnectAsync();
                    kvp.Value.Dispose();
                    _logger.LogInformation("设备 [{DeviceCode}] 已断开并释放", kvp.Key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "设备 [{DeviceCode}] 关闭异常", kvp.Key);
                }
            }

            _devices.Clear();
            _isInitialized = false;

            _logger.LogInformation("========== 设备管理器已关闭 ==========");
        }

        public T? GetDevice<T>(string deviceCode) where T : class, IDevice
        {
            if (_devices.TryGetValue(deviceCode, out var device) && device is T typedDevice)
            {
                return typedDevice;
            }

            _logger.LogWarning("未找到设备 [{DeviceCode}] 或其类型不匹配", deviceCode);
            return null;
        }

        public IReadOnlyList<T> GetAllDevices<T>() where T : class, IDevice
        {
            return _devices.Values
                .OfType<T>()
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<ICamera> GetCameras()
        {
            return GetAllDevices<ICamera>();
        }

        public IReadOnlyList<IMotionCard> GetMotionCards()
        {
            return GetAllDevices<IMotionCard>();
        }

        public IReadOnlyList<IDevice> GetAllDevices()
        {
            return _devices.Values.ToList().AsReadOnly();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    ShutdownAllAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // 忽略 dispose 中的异常
                }
            }

            _disposed = true;
        }

        #endregion
    }
}
