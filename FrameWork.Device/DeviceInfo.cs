namespace FrameWork.Device
{
    /// <summary>
    /// 设备元数据信息
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>设备唯一编码</summary>
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>设备显示名称</summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>设备类型</summary>
        public DeviceType DeviceType { get; set; }

        /// <summary>厂商名称</summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>设备型号</summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>固件/驱动版本</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>序列号</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>描述信息</summary>
        public string Description { get; set; } = string.Empty;
    }
}
