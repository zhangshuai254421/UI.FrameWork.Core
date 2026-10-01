namespace Framework.Device.Domain
{
    /// <summary>
    /// 标记一个厂商适配器类，声明它驱动哪类设备的哪个厂商。
    /// 适配器工厂据此发现并建立「(设备类型, 厂商) → 适配器」的注册映射，无需硬引用具体厂商。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DeviceAdapterAttribute : Attribute
    {
        /// <summary>标记适配器驱动的设备类型与厂商。</summary>
        public DeviceAdapterAttribute(DeviceKind kind, string vendor)
        {
            Kind = kind;
            Vendor = vendor;
        }

        /// <summary>设备类型：相机 / 运动控制卡 / 其他。</summary>
        public DeviceKind Kind { get; }

        /// <summary>厂商：SDK 提供方。</summary>
        public string Vendor { get; }
    }
}
