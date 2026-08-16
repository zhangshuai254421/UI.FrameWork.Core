using EFCore.IRepository;
using FrameWork.Device;

namespace Device.Domain
{
    /// <summary>
    /// 设备配置服务接口
    /// </summary>
    public interface IDeviceConfigurationService : IEntityServiceBase<DeviceConfiguration, int>
    {
        /// <summary>
        /// 获取所有启用的设备配置（按 SortOrder 排序）
        /// </summary>
        Task<IEnumerable<DeviceConfiguration>> GetEnabledDevicesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定类型的启用设备配置
        /// </summary>
        Task<IEnumerable<DeviceConfiguration>> GetEnabledDevicesByTypeAsync(
            DeviceType deviceType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据设备编码获取配置
        /// </summary>
        Task<DeviceConfiguration?> GetByDeviceCodeAsync(
            string deviceCode,
            CancellationToken cancellationToken = default);
    }
}
