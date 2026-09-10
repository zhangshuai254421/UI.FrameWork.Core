using Framework.Device.HikVision;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// CameraData 与 PixelType 的基础数据测试（不依赖任何硬件/原生 SDK）。
    /// </summary>
    public class CameraDataTests
    {
        [Fact]
        public void 默认构造_字段为初始值()
        {
            var data = new CameraData();

            Assert.Equal(IntPtr.Zero, data.ImageAddr);
            Assert.Null(data.ImageData);
            Assert.Equal((ushort)0, data.nWidth);
            Assert.Equal((ushort)0, data.nHeight);
            // 注意：nPixelType 是枚举，默认值为 0（PixelType 未定义值为 0 的成员），
            // 而不是 PixelType_Gvsp_Undefined(-1)
            Assert.Equal(0, (int)data.nPixelType);
        }

        [Theory]
        [InlineData(PixelType.PixelType_Gvsp_Undefined, -1)]
        [InlineData(PixelType.PixelType_Gvsp_Mono8, 17301505)]
        [InlineData(PixelType.PixelType_Gvsp_Mono10_Packed, 17563652)]
        [InlineData(PixelType.PixelType_Gvsp_RGB8_Packed, 35127316)]
        [InlineData(PixelType.PixelType_Gvsp_BGR8_Packed, 35127317)]
        public void PixelType_枚举值符合GigEVision标准(PixelType pixelType, int expected)
        {
            Assert.Equal(expected, (int)pixelType);
        }

        [Fact]
        public void DriverLibResult_枚举值从NoError开始递增()
        {
            Assert.Equal(0, (int)DriverLibResult.DriverLibNoError);
            Assert.Equal(1, (int)DriverLibResult.DriverLibError);
            Assert.Equal(2, (int)DriverLibResult.DriverLibDeviceNotOpen);
        }
    }
}
