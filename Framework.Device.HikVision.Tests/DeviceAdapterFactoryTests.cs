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
        /// <summary>用带 DeviceAdapter 特性的类型构造工厂，预期按特性声明的类型与厂商解析并实例化出该适配器。</summary>
        [Fact]
        public void 从特性发现适配器类型并建立注册映射()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter) });

            var device = factory.Create(DeviceKind.Camera, "FakeVendor");

            Assert.IsType<FakeAdapter>(device);
        }

        /// <summary>注册相机与运动控制卡两个假适配器，预期同一厂商下按设备类型分别解析出对应实例。</summary>
        [Fact]
        public void 按设备类型与厂商共同解析适配器()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter), typeof(FakeMotionCard) });

            var camera = factory.Create(DeviceKind.Camera, "FakeVendor");
            var motion = factory.Create(DeviceKind.MotionControlCard, "FakeVendor");

            Assert.IsType<FakeAdapter>(camera);
            Assert.IsType<FakeMotionCard>(motion);
        }

        /// <summary>请求未注册的厂商，预期创建失败并抛出 InvalidOperationException。</summary>
        [Fact]
        public void 厂商未注册时抛异常()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter) });

            Assert.Throws<InvalidOperationException>(() => factory.Create(DeviceKind.Camera, "NoSuchVendor"));
        }

        /// <summary>连续创建两次同一适配器，预期得到两个互不相同的独立实例。</summary>
        [Fact]
        public void 每次创建返回独立实例()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(FakeAdapter) });

            var first = factory.Create(DeviceKind.Camera, "FakeVendor");
            var second = factory.Create(DeviceKind.Camera, "FakeVendor");

            Assert.NotSame(first, second);
        }

        /// <summary>只注册未标记 DeviceAdapter 特性的类型，预期工厂解析不到适配器而抛出 InvalidOperationException。</summary>
        [Fact]
        public void 无特性标记的类型被忽略()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(NotAnAdapter) });

            Assert.Throws<InvalidOperationException>(() => factory.Create(DeviceKind.Camera, "FakeVendor"));
        }

        /// <summary>用真实海康适配器类型构造工厂，预期可通过特性发现并无参实例化（仅验证类型解析，不打开硬件）。</summary>
        [Fact]
        public void 真实海康适配器可通过特性发现并无参实例化()
        {
            var factory = new DeviceAdapterFactory(new[] { typeof(CameraHikVision) });

            var device = factory.Create(DeviceKind.Camera, "HikVision");

            Assert.IsType<CameraHikVision>(device);
        }

        /// <summary>测试用假相机适配器：注册为相机类型的 FakeVendor。</summary>
        [DeviceAdapter(DeviceKind.Camera, "FakeVendor")]
        private sealed class FakeAdapter : DeviceBase
        {
            public override string Name => "FakeAdapter";

            public override bool Open(DeviceConnection connection) => true;

            public override void Close()
            {
            }
        }

        /// <summary>测试用假运动控制卡适配器：注册为运动控制卡类型的 FakeVendor。</summary>
        [DeviceAdapter(DeviceKind.MotionControlCard, "FakeVendor")]
        private sealed class FakeMotionCard : DeviceBase
        {
            public override string Name => "FakeMotionCard";

            public override bool Open(DeviceConnection connection) => true;

            public override void Close()
            {
            }
        }

        /// <summary>未标记 DeviceAdapter 特性的普通类型，用于验证工厂会忽略它。</summary>
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
