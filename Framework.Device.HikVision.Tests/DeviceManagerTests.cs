using Framework.Device.Domain;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 设备管理器测试：内存配置 → 解析出正确适配器，不加载真实 DLL（缝②）。
    /// 用假适配器工厂与假设备验证外部行为：解析、打开、降级、按标识取用、退出统一关闭。
    /// </summary>
    public class DeviceManagerTests
    {
        [Fact]
        public void 配置实体携带设备标识类型厂商与连接方式()
        {
            var config = new DeviceConfiguration(
                "cam1", DeviceKind.Camera, "HikVision", new TcpConnection("192.168.1.64", 8000));

            Assert.Equal("cam1", config.Id);
            Assert.Equal(DeviceKind.Camera, config.Kind);
            Assert.Equal("HikVision", config.Vendor);
            Assert.IsType<TcpConnection>(config.Connection);
        }

        [Fact]
        public void 初始化按配置解析出正确适配器并以连接方式打开()
        {
            var hikCam = new RecordingDevice("hik");
            var baslerCam = new RecordingDevice("basler");
            var factory = new FakeDeviceAdapterFactory((kind, vendor) =>
                vendor == "HikVision" ? hikCam : baslerCam);
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("cam1", DeviceKind.Camera, "HikVision", new TcpConnection("192.168.1.64", 8000)),
                new DeviceConfiguration("cam2", DeviceKind.Camera, "Basler", new ComConnection("COM3")),
            });

            Assert.Equal(2, factory.CreateCalls.Count);
            Assert.Contains((DeviceKind.Camera, "HikVision"), factory.CreateCalls);
            Assert.Contains((DeviceKind.Camera, "Basler"), factory.CreateCalls);

            Assert.True(hikCam.IsOpen);
            Assert.Equal("192.168.1.64", Assert.IsType<TcpConnection>(hikCam.OpenedConnection).IpAddress);

            Assert.True(baslerCam.IsOpen);
            Assert.Equal("COM3", Assert.IsType<ComConnection>(baslerCam.OpenedConnection).PortName);
        }

        [Fact]
        public void 按设备类型与厂商共同解析适配器()
        {
            var motion = new RecordingDevice("motion");
            var factory = new FakeDeviceAdapterFactory((kind, vendor) =>
                kind == DeviceKind.MotionControlCard ? motion : new RecordingDevice("cam"));
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("axis", DeviceKind.MotionControlCard, "Leisai", new ComConnection("COM1")),
            });

            Assert.Contains((DeviceKind.MotionControlCard, "Leisai"), factory.CreateCalls);
            Assert.Same(motion, manager.GetDevice("axis"));
        }

        [Fact]
        public void 同一厂商多台设备各自独立配置与打开()
        {
            var created = new List<RecordingDevice>();
            var factory = new FakeDeviceAdapterFactory((kind, vendor) =>
            {
                var device = new RecordingDevice($"{vendor}-{created.Count}");
                created.Add(device);
                return device;
            });
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("cam1", DeviceKind.Camera, "HikVision", new TcpConnection("192.168.1.64", 8000)),
                new DeviceConfiguration("cam2", DeviceKind.Camera, "HikVision", new TcpConnection("192.168.1.65", 8000)),
            });

            Assert.Equal(2, created.Count);
            Assert.NotSame(created[0], created[1]);
            Assert.Same(created[0], manager.GetDevice("cam1"));
            Assert.Same(created[1], manager.GetDevice("cam2"));
        }

        [Fact]
        public void 单台打开失败时降级运行其余照常()
        {
            var failing = new RecordingDevice("failing") { OpenResult = false };
            var ok = new RecordingDevice("ok");
            var factory = new FakeDeviceAdapterFactory((kind, vendor) =>
                vendor == "Failing" ? failing : ok);
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("bad", DeviceKind.Camera, "Failing", new TcpConnection("1.1.1.1", 1)),
                new DeviceConfiguration("good", DeviceKind.Camera, "HikVision", new TcpConnection("2.2.2.2", 2)),
            });

            Assert.Null(manager.GetDevice("bad"));
            Assert.Same(ok, manager.GetDevice("good"));
            Assert.True(ok.IsOpen);
        }

        [Fact]
        public void 打开失败的设备被关闭释放部分资源()
        {
            var failing = new RecordingDevice("failing") { OpenResult = false };
            var factory = new FakeDeviceAdapterFactory((kind, vendor) => failing);
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("bad", DeviceKind.Camera, "Failing", new TcpConnection("1.1.1.1", 1)),
            });

            Assert.Null(manager.GetDevice("bad"));
            Assert.True(failing.WasClosed);
        }

        [Fact]
        public void 厂商未注册时降级运行不中断其余设备()
        {
            var ok = new RecordingDevice("ok");
            var factory = new FakeDeviceAdapterFactory((kind, vendor) =>
                vendor == "HikVision" ? ok : throw new InvalidOperationException($"未注册适配器 ({kind}, {vendor})"));
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("unknown", DeviceKind.Camera, "NoSuchVendor", new TcpConnection("3.3.3.3", 3)),
                new DeviceConfiguration("good", DeviceKind.Camera, "HikVision", new TcpConnection("4.4.4.4", 4)),
            });

            Assert.Null(manager.GetDevice("unknown"));
            Assert.Same(ok, manager.GetDevice("good"));
            Assert.True(ok.IsOpen);
        }

        [Fact]
        public void 通过设备标识取得已初始化设备()
        {
            var device = new RecordingDevice("cam");
            var factory = new FakeDeviceAdapterFactory((kind, vendor) => device);
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("cam1", DeviceKind.Camera, "HikVision", new TcpConnection("192.168.1.64", 8000)),
            });

            Assert.Same(device, manager.GetDevice("cam1"));
            Assert.Null(manager.GetDevice("nope"));
        }

        [Fact]
        public void 退出时统一关闭所有已打开设备()
        {
            var a = new RecordingDevice("a");
            var b = new RecordingDevice("b");
            var queue = new Queue<RecordingDevice>(new[] { a, b });
            var factory = new FakeDeviceAdapterFactory((kind, vendor) => queue.Dequeue());
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("a", DeviceKind.Camera, "HikVision", new TcpConnection("1.1.1.1", 1)),
                new DeviceConfiguration("b", DeviceKind.Camera, "HikVision", new TcpConnection("2.2.2.2", 2)),
            });

            Assert.True(a.IsOpen);
            Assert.True(b.IsOpen);

            manager.CloseAll();

            Assert.False(a.IsOpen);
            Assert.False(b.IsOpen);
            Assert.Null(manager.GetDevice("a"));
            Assert.Null(manager.GetDevice("b"));
        }

        [Fact]
        public void Dispose关闭所有设备()
        {
            var a = new RecordingDevice("a");
            var factory = new FakeDeviceAdapterFactory((kind, vendor) => a);
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("a", DeviceKind.Camera, "HikVision", new TcpConnection("1.1.1.1", 1)),
            });

            manager.Dispose();

            Assert.False(a.IsOpen);
            Assert.Null(manager.GetDevice("a"));
        }

        [Fact]
        public void 设备标识重复时关闭旧实例避免泄漏()
        {
            var created = new List<RecordingDevice>();
            var factory = new FakeDeviceAdapterFactory((kind, vendor) =>
            {
                var device = new RecordingDevice("cam");
                created.Add(device);
                return device;
            });
            var manager = new DeviceManager(factory);

            manager.Initialize(new[]
            {
                new DeviceConfiguration("cam1", DeviceKind.Camera, "HikVision", new TcpConnection("1.1.1.1", 1)),
                new DeviceConfiguration("cam1", DeviceKind.Camera, "HikVision", new TcpConnection("2.2.2.2", 2)),
            });

            Assert.Equal(2, created.Count);
            Assert.False(created[0].IsOpen);
            Assert.Same(created[1], manager.GetDevice("cam1"));

            manager.CloseAll();
            Assert.False(created[1].IsOpen);
        }

        [Fact]
        public void 关闭后可重新初始化重连()
        {
            var created = new List<RecordingDevice>();
            var factory = new FakeDeviceAdapterFactory((kind, vendor) =>
            {
                var device = new RecordingDevice("cam");
                created.Add(device);
                return device;
            });
            var manager = new DeviceManager(factory);
            var config = new DeviceConfiguration("cam1", DeviceKind.Camera, "HikVision", new TcpConnection("192.168.1.64", 8000));

            manager.Initialize(new[] { config });
            Assert.True(created[0].IsOpen);

            manager.CloseAll();
            Assert.False(created[0].IsOpen);

            manager.Initialize(new[] { config });
            Assert.Equal(2, created.Count);
            Assert.True(created[1].IsOpen);
            Assert.Same(created[1], manager.GetDevice("cam1"));
        }

        /// <summary>假适配器工厂：记录每次 (设备类型, 厂商) 解析，按委托产出假设备，未注册则抛异常。</summary>
        private sealed class FakeDeviceAdapterFactory : IDeviceAdapterFactory
        {
            private readonly Func<DeviceKind, string, IDevice>? _creator;

            public FakeDeviceAdapterFactory(Func<DeviceKind, string, IDevice>? creator = null) => _creator = creator;

            public List<(DeviceKind Kind, string Vendor)> CreateCalls { get; } = new();

            public IDevice Create(DeviceKind kind, string vendor)
            {
                CreateCalls.Add((kind, vendor));
                return _creator?.Invoke(kind, vendor)
                    ?? throw new InvalidOperationException($"未注册适配器 ({kind}, {vendor})");
            }
        }

        /// <summary>假设备：记录被打开时的连接方式与开关状态，可配置打开结果。</summary>
        private sealed class RecordingDevice : IDevice
        {
            public RecordingDevice(string name) => Name = name;

            public string Name { get; }

            public DeviceConnection? OpenedConnection { get; private set; }

            public bool IsOpen { get; private set; }

            public bool OpenResult { get; set; } = true;

            public bool WasClosed { get; private set; }

            public bool Open(DeviceConnection connection)
            {
                OpenedConnection = connection;
                IsOpen = OpenResult;
                return OpenResult;
            }

            public void Close()
            {
                IsOpen = false;
                WasClosed = true;
            }
        }
    }
}
