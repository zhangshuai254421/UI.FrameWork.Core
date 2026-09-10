using System.Runtime.CompilerServices;
using Framework.Device.HikVision;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// CheckFuncReturnCode 的返回码映射测试。
    /// 通过 RuntimeHelpers 创建"未初始化实例"，绕过构造器中
    /// 对原生 SDK 对象（CCamera/deviceInfo）的字段初始化，测试无需真实相机。
    /// </summary>
    public class CheckFuncReturnCodeTests
    {
        private static CameraHikVision CreateUninitializedCamera() =>
            (CameraHikVision)RuntimeHelpers.GetUninitializedObject(typeof(CameraHikVision));

        [Theory]
        [InlineData(0, DriverLibResult.DriverLibNoError)]
        [InlineData(-2147483648, DriverLibResult.DriverLibError)] // MV_E_HANDLE
        [InlineData(-2147483393, DriverLibResult.DriverLibError)] // MV_E_UNKNOW
        [InlineData(1, DriverLibResult.DriverLibError)]
        [InlineData(int.MaxValue, DriverLibResult.DriverLibError)]
        public void CheckFuncReturnCode_返回码映射(int returnCode, DriverLibResult expected)
        {
            var camera = CreateUninitializedCamera();

            var result = camera.CheckFuncReturnCode(returnCode, "UnitTest");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CheckFuncReturnCode_成功码恒为NoError()
        {
            var camera = CreateUninitializedCamera();

            Assert.Equal(DriverLibResult.DriverLibNoError,
                camera.CheckFuncReturnCode(0, nameof(ICamera.Open)));
        }
    }
}
