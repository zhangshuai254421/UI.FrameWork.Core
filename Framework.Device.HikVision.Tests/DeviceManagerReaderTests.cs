using Framework.Device;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 设备管理器从配置读取器（数据库来源）读配置的契约测试。
    /// </summary>
    public class DeviceManagerReaderTests
    {
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

        [Fact]
        public void 读取器为null时抛参数异常()
        {
            var manager = new DeviceManager(new StubFactory(new RecordingDevice("cam")));

            Assert.Throws<ArgumentNullException>(() => manager.Initialize((IDeviceConfigurationReader)null!));
        }

        private sealed class StubReader : IDeviceConfigurationReader
        {
            private readonly IReadOnlyList<DeviceConfiguration> _configs;

            public StubReader(IReadOnlyList<DeviceConfiguration> configs) => _configs = configs;

            public IReadOnlyList<DeviceConfiguration> Read() => _configs;
        }

        private sealed class StubFactory : IDeviceAdapterFactory
        {
            private readonly IDevice _device;

            public StubFactory(IDevice device) => _device = device;

            public IDevice Create(DeviceKind kind, string vendor) => _device;
        }

        private sealed class RecordingDevice : IDevice
        {
            public RecordingDevice(string name) => Name = name;

            public string Name { get; }

            public bool IsOpen { get; private set; }

            public bool Open(DeviceConnection connection) => IsOpen = true;

            public void Close() => IsOpen = false;
        }
    }
}
