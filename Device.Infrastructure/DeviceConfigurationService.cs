using Device.Domain;
using EFCore.Infrastructure;
using EFCore.Repository;
using FrameWork.Device;

namespace Device.Infrastructure
{
    /// <summary>
    /// 设备配置 CRUD 服务实现
    /// </summary>
    public class DeviceConfigurationService :
        EntityServiceBase<DeviceConfiguration, int>,
        IDeviceConfigurationService
    {
        #region 构造函数

        public DeviceConfigurationService(
            IUnitOfWork unitOfWork)
            : base(unitOfWork)
        {
        }

        #endregion

        #region IDeviceConfigurationService 实现

        public async Task<IEnumerable<DeviceConfiguration>> GetEnabledDevicesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _repository.GetListAsync(
                d => d.IsEnabled,
                cancellationToken);
        }

        public async Task<IEnumerable<DeviceConfiguration>> GetEnabledDevicesByTypeAsync(
            DeviceType deviceType,
            CancellationToken cancellationToken = default)
        {
            return await _repository.GetListAsync(
                d => d.IsEnabled && d.DeviceType == deviceType,
                cancellationToken);
        }

        public async Task<DeviceConfiguration?> GetByDeviceCodeAsync(
            string deviceCode,
            CancellationToken cancellationToken = default)
        {
            return await _repository.FindAsync(
                d => d.DeviceCode == deviceCode,
                cancellationToken);
        }

        #endregion
    }
}
