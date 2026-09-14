namespace Framework.Device
{
    /// <summary>
    /// 设备级配置：一台设备如何被抵达与驱动，字段为设备标识、设备类型、厂商、连接方式。
    /// 品牌无关，供设备管理器读取；与配方参数中的相机引用是两回事。
    /// </summary>
    public class DeviceConfiguration
    {
        public DeviceConfiguration(string id, DeviceKind kind, string vendor, DeviceConnection connection)
        {
            Id = id;
            Kind = kind;
            Vendor = vendor;
            Connection = connection;
        }

        /// <summary>设备标识：逻辑名，配方或流程据此引用一台设备。</summary>
        public string Id { get; }

        /// <summary>设备类型：相机 / 运动控制卡 / 其他。</summary>
        public DeviceKind Kind { get; }

        /// <summary>厂商：SDK 提供方，决定加载哪套驱动适配器。</summary>
        public string Vendor { get; }

        /// <summary>连接方式：TCP（IP + 端口）或 COM（串口号）。</summary>
        public DeviceConnection Connection { get; }
    }
}
