using Framework.Device.Domain;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 设备管理器从配置读取器（数据库来源）读配置的契约测试。
    /// </summary>
    public class DeviceManagerReaderTests
    {
        /// <summary>用桩读取器提供一条相机配置并初始化管理器，预期按配置打开设备且能按 Id 取到同一实例。</summary>
        [Fact]
        public void 从读取器读取配置并初始化设备()
        {
            var opened = new RecordingDevice("cam");
            var manager = new DeviceManager(new StubFactory(opened));
            var reader = new StubReader(new[]
            {
                new DeviceConfiguration("cam1", DeviceKind.Camera, "HikVision", new TcpConnection("192.168.1.64", 8000)),
            });

            manager.Initialize(reader);

            Assert.Same(opened, manager.GetDevice("cam1"));
            Assert.True(opened.IsOpen);
        }

        /// <summary>向 Initialize 传入 null 读取器，预期抛出 ArgumentNullException。</summary>
        [Fact]
        public void 读取器为null时抛参数异常()
        {
            var manager = new DeviceManager(new StubFactory(new RecordingDevice("cam")));

            Assert.Throws<ArgumentNullException>(() => manager.Initialize((IDeviceConfigurationReader)null!));
        }

        /// <summary>桩读取器：始终返回预置的配置列表。</summary>
        private sealed class StubReader : IDeviceConfigurationReader
        {
            /// <summary>构造时固定的配置列表。</summary>
            private readonly IReadOnlyList<DeviceConfiguration> _configs;

            /// <summary>用给定配置列表构造桩读取器。</summary>
            public StubReader(IReadOnlyList<DeviceConfiguration> configs) => _configs = configs;

            /// <inheritdoc />
            public IReadOnlyList<DeviceConfiguration> Read() => _configs;
        }

        /// <summary>桩工厂：无论请求什么设备类型与厂商，都返回构造时注入的同一设备实例。</summary>
        private sealed class StubFactory : IDeviceAdapterFactory
        {
            /// <summary>构造时注入的固定设备实例。</summary>
            private readonly IDevice _device;

            /// <summary>用给定设备实例构造桩工厂。</summary>
            public StubFactory(IDevice device) => _device = device;

            /// <inheritdoc />
            public IDevice Create(DeviceKind kind, string vendor) => _device;
        }

        /// <summary>记录型假设备：实现 IDevice，用布尔标志记录打开与关闭状态。</summary>
        private sealed class RecordingDevice : IDevice
        {
            /// <summary>用给定设备名构造假设备。</summary>
            public RecordingDevice(string name) => Name = name;

            /// <inheritdoc />
            public string Name { get; }

            /// <summary>设备当前是否已打开。</summary>
            public bool IsOpen { get; private set; }

            /// <inheritdoc />
            public bool Open(DeviceConnection connection) => IsOpen = true;

            /// <inheritdoc />
            public void Close() => IsOpen = false;
        }
    }
}
