using System.Collections.Concurrent;
using System.Reflection;
using Device.Domain;
using FrameWork.Device;
using Microsoft.Extensions.Logging;

namespace Device.Infrastructure
{
    /// <summary>
    /// 设备插件加载器 - 根据数据库配置动态加载外部硬件 DLL
    /// </summary>
    public class DevicePluginLoader
    {
        #region 字段

        private readonly IDeviceConfigurationService _configService;
        private readonly ILogger<DevicePluginLoader> _logger;
        private readonly string _pluginsFolderPath;

        #endregion

        #region 构造函数

        public DevicePluginLoader(
            IDeviceConfigurationService configService,
            ILogger<DevicePluginLoader> logger,
            string? pluginsFolderPath = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pluginsFolderPath = pluginsFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载所有已启用的设备插件
        /// </summary>
        /// <returns>成功加载的设备实例列表</returns>
        public async Task<IReadOnlyList<IDevice>> LoadDevicePluginsAsync(
            CancellationToken cancellationToken = default)
        {
            var devices = new List<IDevice>();

            // 1. 从数据库获取所有启用设备配置（按 SortOrder 排序）
            var configs = await _configService.GetEnabledDevicesAsync(cancellationToken);
            var sortedConfigs = configs.OrderBy(c => c.SortOrder).ToList();

            _logger.LogInformation("发现 {Count} 个已启用的设备配置，开始加载...", sortedConfigs.Count);

            // 2. 逐个加载
            foreach (var config in sortedConfigs)
            {
                var device = await LoadSingleDeviceAsync(config);
                if (device != null)
                {
                    devices.Add(device);
                }
            }

            _logger.LogInformation("设备插件加载完成，成功 {Success}/{Total}",
                devices.Count, sortedConfigs.Count);

            return devices.AsReadOnly();
        }

        #endregion

        #region 私有方法

        private async Task<IDevice?> LoadSingleDeviceAsync(DeviceConfiguration config)
        {
            try
            {
                _logger.LogInformation("正在加载设备 [{DeviceCode}] {DeviceName} (类型: {DeviceType})",
                    config.DeviceCode, config.DeviceName, config.DeviceType);

                // 2a. 验证配置
                if (string.IsNullOrWhiteSpace(config.AssemblyFileName))
                {
                    _logger.LogError("设备 [{DeviceCode}] AssemblyFileName 为空，跳过", config.DeviceCode);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(config.ClassFullName))
                {
                    _logger.LogError("设备 [{DeviceCode}] ClassFullName 为空，跳过", config.DeviceCode);
                    return null;
                }

                // 2b. 加载程序集
                var assemblyPath = ResolveAssemblyPath(config.AssemblyFileName);
                if (!File.Exists(assemblyPath))
                {
                    _logger.LogError("设备 [{DeviceCode}] DLL 文件不存在: {Path}", config.DeviceCode, assemblyPath);
                    return null;
                }

                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(assemblyPath);
                    _logger.LogDebug("设备 [{DeviceCode}] 程序集加载成功: {Assembly}",
                        config.DeviceCode, assembly.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "设备 [{DeviceCode}] 程序集加载失败: {Path}", config.DeviceCode, assemblyPath);
                    return null;
                }

                // 2c. 获取类型
                var deviceType = assembly.GetType(config.ClassFullName);
                if (deviceType == null)
                {
                    _logger.LogError("设备 [{DeviceCode}] 找不到类型 {TypeName} in {Assembly}",
                        config.DeviceCode, config.ClassFullName, assemblyPath);
                    return null;
                }

                // 2d. 验证接口实现
                var expectedInterface = GetExpectedInterfaceType(config.DeviceType);
                if (expectedInterface != null && !expectedInterface.IsAssignableFrom(deviceType))
                {
                    _logger.LogError(
                        "设备 [{DeviceCode}] 类型 {TypeName} 未实现 {InterfaceName} 接口",
                        config.DeviceCode, config.ClassFullName, expectedInterface.Name);
                    return null;
                }

                if (!typeof(IDevice).IsAssignableFrom(deviceType))
                {
                    _logger.LogError(
                        "设备 [{DeviceCode}] 类型 {TypeName} 未实现 IDevice 接口",
                        config.DeviceCode, config.ClassFullName);
                    return null;
                }

                // 2e. 创建实例
                if (Activator.CreateInstance(deviceType) is not IDevice device)
                {
                    _logger.LogError("设备 [{DeviceCode}] 创建实例失败", config.DeviceCode);
                    return null;
                }

                // 2f. 设置基础属性（通过反射设置 DeviceBase 中的属性）
                SetDeviceProperty(deviceType, device, nameof(IDevice.DeviceCode), config.DeviceCode);
                SetDeviceProperty(deviceType, device, nameof(IDevice.DeviceName), config.DeviceName);

                // 2g. 设置连接参数（如果设备有 ConnectionParams 属性）
                if (!string.IsNullOrWhiteSpace(config.ConnectionParams))
                {
                    SetDeviceProperty(deviceType, device, "ConnectionParams", config.ConnectionParams);
                }

                _logger.LogInformation("设备 [{DeviceCode}] {DeviceName} 插件实例创建成功",
                    config.DeviceCode, config.DeviceName);

                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备 [{DeviceCode}] 加载过程发生未预期异常", config.DeviceCode);
                return null;
            }
        }

        /// <summary>
        /// 解析 DLL 完整路径
        /// </summary>
        private string ResolveAssemblyPath(string assemblyFileName)
        {
            // 如果是绝对路径，直接返回
            if (Path.IsPathRooted(assemblyFileName))
                return assemblyFileName;

            // 否则拼接到 Plugins 文件夹
            return Path.Combine(_pluginsFolderPath, assemblyFileName);
        }

        /// <summary>
        /// 根据设备类型获取期望的接口类型
        /// </summary>
        private static Type? GetExpectedInterfaceType(DeviceType deviceType)
        {
            return deviceType switch
            {
                DeviceType.Camera => typeof(ICamera),
                DeviceType.MotionCard => typeof(IMotionCard),
                _ => null  // PLC、Sensor、Other 等目前仅要求 IDevice
            };
        }

        /// <summary>
        /// 通过反射设置设备属性值
        /// </summary>
        private static void SetDeviceProperty(Type deviceType, IDevice device, string propertyName, string value)
        {
            try
            {
                var prop = deviceType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                {
                    prop.SetValue(device, value);
                }
            }
            catch
            {
                // 忽略反射设置属性时的异常（设备不一定有这个属性）
            }
        }

        #endregion
    }
}
