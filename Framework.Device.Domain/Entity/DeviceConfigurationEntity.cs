using Framework.Device;

namespace Framework.Device.Domain.Entity
{
    /// <summary>
    /// 设备配置持久化实体：一台物理设备如何被抵达与驱动。扁平标量字段便于入库。
    /// 注意：它与配方参数里的相机引用（Recipe.Domain.CameraConfiguration）是两回事——
    /// 前者回答「这台设备怎么连」，后者回答「这一步用哪台相机」，仅通过设备标识这个字符串弱关联。
    /// </summary>
    public class DeviceConfigurationEntity
    {
        /// <summary>设备标识（逻辑名，主键）。配方/流程据此引用一台设备。</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>设备类型：相机 / 运动控制卡 / 其他。</summary>
        public DeviceKind Kind { get; set; }

        /// <summary>厂商：SDK 提供方，决定加载哪套驱动适配器。</summary>
        public string Vendor { get; set; } = string.Empty;

        /// <summary>连接方式：TCP 或 COM。</summary>
        public ConnectionType ConnectionType { get; set; }

        /// <summary>TCP 地址（ConnectionType=Tcp 时有效）。</summary>
        public string? IpAddress { get; set; }

        /// <summary>TCP 端口（ConnectionType=Tcp 时有效）。</summary>
        public int? Port { get; set; }

        /// <summary>COM 口号（ConnectionType=Com 时有效）。</summary>
        public string? PortName { get; set; }

        /// <summary>映射为设备管理器消费的契约配置对象。</summary>
        public DeviceConfiguration ToDeviceConfiguration()
        {
            DeviceConnection connection = ConnectionType switch
            {
                ConnectionType.Tcp => new TcpConnection(IpAddress ?? string.Empty, Port ?? 0),
                ConnectionType.Com => new ComConnection(PortName ?? string.Empty),
                _ => throw new InvalidOperationException($"未知连接方式 {ConnectionType}"),
            };

            return new DeviceConfiguration(Id, Kind, Vendor, connection);
        }
    }
}
