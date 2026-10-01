using Framework.Device.Domain;
using Framework.Device.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 设备配置读取器测试：用内存 SQLite 真实验证「从数据库读配置」这一外部行为（缝③）。
    /// </summary>
    public class DeviceConfigurationReaderTests
    {
        [Fact]
        public void 从数据库读取TCP配置并映射为契约对象()
        {
            using var connection = OpenInMemory();

            using (var context = CreateContext(connection))
            {
                context.Database.EnsureCreated();
            }

            using (var context = CreateContext(connection))
            {
                var configs = new DeviceConfigurationReader(context).Read();

                var camera = Assert.Single(configs, c => c.Id == "CameraA");
                Assert.Equal(DeviceKind.Camera, camera.Kind);
                Assert.Equal("HikVision", camera.Vendor);

                var tcp = Assert.IsType<TcpConnection>(camera.Connection);
                Assert.Equal("192.168.1.64", tcp.IpAddress);
                Assert.Equal(8000, tcp.Port);
            }
        }

        [Fact]
        public void 从数据库读取COM配置并映射为契约对象()
        {
            using var connection = OpenInMemory();

            using (var context = CreateContext(connection))
            {
                context.Database.EnsureCreated();
            }

            using (var context = CreateContext(connection))
            {
                var configs = new DeviceConfigurationReader(context).Read();

                var motion = Assert.Single(configs, c => c.Id == "MotionCard1");
                Assert.Equal(DeviceKind.MotionControlCard, motion.Kind);
                Assert.Equal("Leisai", motion.Vendor);

                var com = Assert.IsType<ComConnection>(motion.Connection);
                Assert.Equal("COM1", com.PortName);
            }
        }

        [Fact]
        public void 数据库种子含相机与运控卡两条配置()
        {
            using var connection = OpenInMemory();

            using (var context = CreateContext(connection))
            {
                context.Database.EnsureCreated();
            }

            using (var context = CreateContext(connection))
            {
                var configs = new DeviceConfigurationReader(context).Read();

                Assert.Equal(2, configs.Count);
            }
        }

        private static SqliteConnection OpenInMemory()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        private static DeviceDataContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<DeviceDataContext>()
                .UseSqlite(connection)
                .Options;
            return new DeviceDataContext(options);
        }
    }
}
