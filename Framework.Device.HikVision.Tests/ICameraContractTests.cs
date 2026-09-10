using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Framework.Device.HikVision;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// ICamera 契约测试：锁定当前"尚未实现"的成员行为。
    /// 当这些成员被实现后，对应测试会失败，提醒同步更新测试。
    /// </summary>
    public class ICameraContractTests
    {
        /// <summary>
        /// Open 已实现（会调用原生 SDK 枚举并打开设备），不能作为"未实现"成员测试。
        /// </summary>
        private static readonly string[] ImplementedMembers = ["Open"];

        private static CameraHikVision CreateUninitializedCamera() =>
            (CameraHikVision)RuntimeHelpers.GetUninitializedObject(typeof(CameraHikVision));

        [Fact]
        public void 测试相机的链接() {
        
            ICamera camera = new CameraHikVision();
            camera.Open();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        [Fact]
        public void 未实现的方法_调用时抛出NotImplementedException()
        {

            var camera = CreateUninitializedCamera();
           

            foreach (var method in typeof(ICamera).GetMethods()
                         .Where(m => !ImplementedMembers.Contains(m.Name)))
            {
                object?[] args = method.GetParameters()
                    .Select(p => BuildDefaultArg(p.ParameterType))
                    .ToArray();

                var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(camera, args));
                Assert.IsType<NotImplementedException>(ex.InnerException);
            }
        }

        [Fact]
        public void ChannelCameraData_属性读写_抛出NotImplementedException()
        {
            var camera = (ICamera)CreateUninitializedCamera();

            Assert.Throws<NotImplementedException>(() => camera.ChannelCameraData);
            Assert.Throws<NotImplementedException>(() =>
            {
                camera.ChannelCameraData = Channel.CreateBounded<CameraData>(1);
            });
        }

        private static object? BuildDefaultArg(Type type)
        {
            if (type.IsByRef)
            {
                var element = type.GetElementType()!;
                return element.IsValueType ? Activator.CreateInstance(element) : null;
            }

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
