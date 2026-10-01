using Framework.Device.Domain;
using Framework.Device.HikVision;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 适配器工厂测试：验证「(设备类型, 厂商) → 适配器」的解析与实例化这一外部行为（缝④）。
    /// 用测试程序集里的假适配器类型替换真实厂商 DLL，不接触真实 SDK 与硬件。
    /// </summary>
    public class DeviceAdapterFactoryTests
    {
        [Fact]
        public void 从特性发现适配器类型并建立注册映射()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter) });

            var device = factory.Create(DeviceKind.Camera, "FakeVendor");

            Assert.IsType<FakeAdapter>(device);
        }

        [Fact]
        public void 按设备类型与厂商共同解析适配器()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter), typeof(FakeMotionCard) });

            var camera = factory.Create(DeviceKind.Camera, "FakeVendor");
            var motion = factory.Create(DeviceKind.MotionControlCard, "FakeVendor");

            Assert.IsType<FakeAdapter>(camera);
            Assert.IsType<FakeMotionCard>(motion);
        }

        [Fact]
        public void 厂商未注册时抛异常()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter) });

            Assert.Throws<InvalidOperationException>(() => factory.Create(DeviceKind.Camera, "NoSuchVendor"));
        }

        [Fact]
        public void 每次创建返回独立实例()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter) });

            var first = factory.Create(DeviceKind.Camera, "FakeVendor");
            var second = factory.Create(DeviceKind.Camera, "FakeVendor");

            Assert.NotSame(first, second);
        }

        [Fact]
        public void 无特性标记的类型被忽略()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(NotAnAdapter) });

            Assert.Throws<InvalidOperationException>(() => factory.Create(DeviceKind.Camera, "FakeVendor"));
        }

        [Fact]
        public void 真实海康适配器可通过特性发现并无参实例化()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(CameraHikVision) });

            var device = factory.Create(DeviceKind.Camera, "HikVision");

            Assert.IsType<CameraHikVision>(device);
        }

        [DeviceAdapter(DeviceKind.Camera, "FakeVendor")]
        private sealed class FakeAdapter : DeviceBase
        {
            public override string Name => "FakeAdapter";

            public override bool Open(DeviceConnection connection) => true;

            public override void Close()
            {
            }
        }

        [DeviceAdapter(DeviceKind.MotionControlCard, "FakeVendor")]
        private sealed class FakeMotionCard : DeviceBase
        {
            public override string Name => "FakeMotionCard";

            public override bool Open(DeviceConnection connection) => true;

            public override void Close()
            {
            }
        }

        private sealed class NotAnAdapter : DeviceBase
        {
            public override string Name => "NotAnAdapter";

            public override bool Open(DeviceConnection connection) => true;

            public override void Close()
            {
            }
        }
    }
}
