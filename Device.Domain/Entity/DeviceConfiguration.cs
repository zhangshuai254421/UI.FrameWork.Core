using EFCore.Repository;
using FrameWork.Device;

namespace Device.Domain
{
    /// <summary>
    /// 设备配置实体 - 存储每个硬件设备的加载配置信息
    /// 对应数据库表 DeviceConfigurations
    /// </summary>
    public class DeviceConfiguration : Entity
    {
        #region 属性

        /// <summary>
        /// 设备唯一编码（如 "CAM-001", "MOT-X"）
        /// </summary>
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>
        /// 设备显示名称
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 设备类型
        /// </summary>
        public DeviceType DeviceType { get; set; }

        /// <summary>
        /// 硬件插件 DLL 文件名（如 "HikRobotCamera.dll"）
        /// 相对于 Plugins/ 文件夹的路径
        /// </summary>
        public string AssemblyFileName { get; set; } = string.Empty;

        /// <summary>
        /// 设备类的完整限定名（如 "HikRobot.HikCamera"）
        /// 用于 Assembly.GetType() 查找
        /// </summary>
        public string ClassFullName { get; set; } = string.Empty;

        /// <summary>
        /// 连接参数字符串（JSON 格式，如 {"IP":"192.168.1.100","Port":5000}）
        /// </summary>
        public string ConnectionParams { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用（false 则跳过加载）
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 加载排序（数值越小越先加载）
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 备注说明
        /// </summary>
        public string? Remark { get; set; }

        #endregion

        #region 构造函数

        public DeviceConfiguration()
        {
        }

        public DeviceConfiguration(
            string deviceCode,
            string deviceName,
            DeviceType deviceType,
            string assemblyFileName,
            string classFullName,
            string connectionParams = "",
            bool isEnabled = true,
            int sortOrder = 0)
        {
            DeviceCode = deviceCode;
            DeviceName = deviceName;
            DeviceType = deviceType;
            AssemblyFileName = assemblyFileName;
            ClassFullName = classFullName;
            ConnectionParams = connectionParams;
            IsEnabled = isEnabled;
            SortOrder = sortOrder;
        }

        #endregion
    }
}
