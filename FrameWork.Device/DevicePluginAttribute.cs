namespace FrameWork.Device
{
    /// <summary>
    /// 标记硬件设备插件类，用于可选的显式声明
    /// （实际发现仍以数据库 DeviceConfiguration 为准，此特性用于文档和辅助验证）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DevicePluginAttribute : Attribute
    {
        /// <summary>设备类型</summary>
        public DeviceType DeviceType { get; }

        /// <summary>插件显示名称</summary>
        public string DisplayName { get; set; }

        /// <summary>插件版本</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>插件描述</summary>
        public string Description { get; set; } = string.Empty;

        public DevicePluginAttribute(DeviceType deviceType, string displayName)
        {
            DeviceType = deviceType;
            DisplayName = displayName;
        }
    }
}
