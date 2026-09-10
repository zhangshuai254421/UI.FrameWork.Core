using Framework.Device;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 海康相机适配器的纯逻辑翻译测试（不接触真实 SDK / 硬件）。
    /// 验证：SDK 错误码 → 统一错误类别；SDK 像素格式 → 品牌无关像素格式。
    /// </summary>
    public class CameraHikVisionTests
    {
        [Theory]
        [InlineData(0, DeviceErrorCategory.None)]                              // MV_OK
        [InlineData(-2147483383, DeviceErrorCategory.ParameterNotSupported)]  // MV_E_GC_NODE_NOT_FOUND
        [InlineData(-2147483644, DeviceErrorCategory.ParameterNotSupported)]  // MV_E_PARAMETER
        [InlineData(-2147483641, DeviceErrorCategory.Unknown)]                // MV_E_NODATA（无数据，非“找不到设备”）
        [InlineData(-2147483130, DeviceErrorCategory.DeviceNotFound)]         // MV_E_NETER
        [InlineData(-2147483133, DeviceErrorCategory.OpenFailed)]             // MV_E_ACCESS_DENIED
        [InlineData(-2147483132, DeviceErrorCategory.OpenFailed)]             // MV_E_BUSY
        [InlineData(-1, DeviceErrorCategory.Unknown)]                         // 未分类错误
        public void 错误码翻译为统一类别(int sdkCode, DeviceErrorCategory expected)
        {
            Assert.Equal(expected, CameraHikVision.TranslateError(sdkCode));
        }

        [Theory]
        [InlineData(17301505, PixelFormat.Mono8)]    // PixelType_Gvsp_Mono8
        [InlineData(17825795, PixelFormat.Mono10)]   // PixelType_Gvsp_Mono10
        [InlineData(17825797, PixelFormat.Mono12)]   // PixelType_Gvsp_Mono12
        [InlineData(17825799, PixelFormat.Mono16)]   // PixelType_Gvsp_Mono16
        [InlineData(17301512, PixelFormat.BayerGR8)] // PixelType_Gvsp_BayerGR8
        [InlineData(17301513, PixelFormat.BayerRG8)] // PixelType_Gvsp_BayerRG8
        [InlineData(17301514, PixelFormat.BayerGB8)] // PixelType_Gvsp_BayerGB8
        [InlineData(17301515, PixelFormat.BayerBG8)] // PixelType_Gvsp_BayerBG8
        [InlineData(35127316, PixelFormat.RGB8)]     // PixelType_Gvsp_RGB8_Packed
        [InlineData(35127317, PixelFormat.BGR8)]     // PixelType_Gvsp_BGR8_Packed
        [InlineData(0, PixelFormat.Undefined)]       // 未匹配
        public void 像素格式翻译为品牌无关格式(int gvspPixelType, PixelFormat expected)
        {
            Assert.Equal(expected, CameraHikVision.TranslatePixelFormat(gvspPixelType));
        }
    }
}
