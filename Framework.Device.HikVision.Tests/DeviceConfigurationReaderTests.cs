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
        /// <summary>向内存 SQLite 写入 TCP 相机配置后读取，预期还原为带 IP 与端口的相机契约配置。</summary>
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

        /// <summary>向内存 SQLite 写入 COM 运控卡配置后读取，预期还原为带串口号的运动控制卡契约配置。</summary>
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

        /// <summary>建库后不写任何数据，预期读取器返回数据库种子自带的相机与运控卡两条配置。</summary>
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

        /// <summary>打开一个共享的内存 SQLite 连接，供同一事务内多次建库与查询。</summary>
        private static SqliteConnection OpenInMemory()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        /// <summary>基于给定 SQLite 连接创建设备数据上下文。</summary>
        private static DeviceDataContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<DeviceDataContext>()
                .UseSqlite(connection)
                .Options;
            return new DeviceDataContext(options);
        }
    }
}
