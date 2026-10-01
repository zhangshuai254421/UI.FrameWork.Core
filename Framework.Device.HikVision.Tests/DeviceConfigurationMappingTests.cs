using Framework.Device.Domain;
using Framework.Device.Infrastructure;
using Framework.Device.Domain.Entity;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 实体 → 契约映射测试：验证扁平持久化字段正确还原为强类型连接对象（不依赖数据库）。
    /// </summary>
    public class DeviceConfigurationMappingTests
    {
        /// <summary>TCP 类型的实体映射为配置对象，预期标识、类型、厂商字段保留且连接对象还原为携带 IP 与端口的 TcpConnection。</summary>
        [Fact]
        public void TCP实体映射为TcpConnection()
        {
            var entity = new DeviceConfigurationEntity
            {
                Id = "cam1",
                Kind = DeviceKind.Camera,
                Vendor = "HikVision",
                ConnectionType = ConnectionType.Tcp,
                IpAddress = "192.168.1.64",
                Port = 8000,
            };

            var config = entity.ToDeviceConfiguration();

            Assert.Equal("cam1", config.Id);
            Assert.Equal(DeviceKind.Camera, config.Kind);
            Assert.Equal("HikVision", config.Vendor);

            var tcp = Assert.IsType<TcpConnection>(config.Connection);
            Assert.Equal("192.168.1.64", tcp.IpAddress);
            Assert.Equal(8000, tcp.Port);
        }

        /// <summary>COM 类型的实体映射为配置对象，预期连接对象还原为携带串口号的 ComConnection。</summary>
        [Fact]
        public void COM实体映射为ComConnection()
        {
            var entity = new DeviceConfigurationEntity
            {
                Id = "axis",
                Kind = DeviceKind.MotionControlCard,
                Vendor = "Leisai",
                ConnectionType = ConnectionType.Com,
                PortName = "COM1",
            };

            var config = entity.ToDeviceConfiguration();

            var com = Assert.IsType<ComConnection>(config.Connection);
            Assert.Equal("COM1", com.PortName);
        }
    }
}
