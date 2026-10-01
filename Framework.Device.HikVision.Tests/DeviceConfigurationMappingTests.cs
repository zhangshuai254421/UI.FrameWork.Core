using Framework.Device;
using Framework.Device.Infrastructure;
using Framework.Device.Infrastructure.Entity;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 实体 → 契约映射测试：验证扁平持久化字段正确还原为强类型连接对象（不依赖数据库）。
    /// </summary>
    public class DeviceConfigurationMappingTests
    {
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
