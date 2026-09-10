using Framework.Device;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// CameraData / PixelFormat / DeviceConnection / DeviceErrorCategory 的基础类型测试（不依赖任何硬件/原生 SDK）。
    /// </summary>
    public class CameraDataTests
    {
        [Fact]
        public void 默认构造_图像数据为空数组_宽高为零()
        {
            var data = new CameraData();

            Assert.NotNull(data.ImageData);
            Assert.Empty(data.ImageData);
            Assert.Equal(0, data.Width);
            Assert.Equal(0, data.Height);
            Assert.Equal(PixelFormat.Undefined, data.PixelFormat);
        }

        [Fact]
        public void 图像数据_为托管字节数组_不暴露裸指针()
        {
            var data = new CameraData
            {
                ImageData = new byte[] { 1, 2, 3 },
                Width = 640,
                Height = 480,
                PixelFormat = PixelFormat.Mono8,
            };

            Assert.Equal(3, data.ImageData.Length);
            Assert.Equal(1, data.ImageData[0]);
            Assert.Equal(2, data.ImageData[1]);
            Assert.Equal(3, data.ImageData[2]);
            Assert.Equal(640, data.Width);
            Assert.Equal(480, data.Height);
            Assert.Equal(PixelFormat.Mono8, data.PixelFormat);
        }

        [Fact]
        public void TcpConnection_携带Ip与端口()
        {
            var tcp = new TcpConnection("192.168.1.64", 8000);

            Assert.Equal("192.168.1.64", tcp.IpAddress);
            Assert.Equal(8000, tcp.Port);
        }

        [Fact]
        public void ComConnection_携带串口号()
        {
            var com = new ComConnection("COM3");

            Assert.Equal("COM3", com.PortName);
        }

        [Fact]
        public void DeviceErrorCategory_含明确的错误类别()
        {
            Assert.Equal(0, (int)DeviceErrorCategory.None);
            Assert.NotEqual((int)DeviceErrorCategory.DeviceNotFound, (int)DeviceErrorCategory.OpenFailed);
            Assert.NotEqual((int)DeviceErrorCategory.OpenFailed, (int)DeviceErrorCategory.ParameterNotSupported);
        }
    }
}
