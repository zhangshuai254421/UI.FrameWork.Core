using Framework.Device.Domain;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// ICamera 契约测试：用假适配器验证真实契约行为（缝①）。
    /// </summary>
    public class CameraContractTests
    {
        [Fact]
        public void 打开时记录连接方式()
        {
            var camera = new FakeCamera();
            var tcp = new TcpConnection("192.168.1.64", 8000);

            Assert.True(camera.Open(tcp));
            Assert.Same(tcp, camera.OpenedConnection);
            Assert.True(camera.IsOpen);
        }

        [Fact]
        public void 曝光与增益可强类型设置并读回()
        {
            var camera = new FakeCamera();

            camera.SetExposureTime(10000.5);
            camera.SetGain(3.2);

            Assert.Equal(10000.5, camera.GetExposureTime());
            Assert.Equal(3.2, camera.GetGain());
        }

        [Fact]
        public void 取一帧返回托管字节数组与宽高格式()
        {
            var camera = new FakeCamera();
            camera.SetNextFrame(new CameraData
            {
                ImageData = new byte[] { 1, 2, 3 },
                Width = 640,
                Height = 480,
                PixelFormat = PixelFormat.Mono8,
            });

            var got = camera.GetOneImage();

            Assert.Equal(3, got.ImageData.Length);
            Assert.Equal(1, got.ImageData[0]);
            Assert.Equal(2, got.ImageData[1]);
            Assert.Equal(3, got.ImageData[2]);
            Assert.Equal(640, got.Width);
            Assert.Equal(480, got.Height);
            Assert.Equal(PixelFormat.Mono8, got.PixelFormat);
        }

        [Fact]
        public async Task 实时图像通道能收到帧()
        {
            var camera = new FakeCamera();
            var frame = new CameraData { ImageData = new byte[] { 9 }, Width = 1, Height = 1 };

            camera.ChannelCameraData.Writer.TryWrite(frame);
            var read = await camera.ChannelCameraData.Reader.ReadAsync();

            Assert.Same(frame, read);
        }

        [Fact]
        public void 软触发计数递增()
        {
            var camera = new FakeCamera();

            camera.TriggerSoftware();
            camera.TriggerSoftware();

            Assert.Equal(2, camera.TriggerCount);
        }

        [Fact]
        public void 开始与停止采集切换状态()
        {
            var camera = new FakeCamera();

            camera.StartAcquisition();
            Assert.True(camera.IsAcquiring);

            camera.StopAcquisition();
            Assert.False(camera.IsAcquiring);
        }
    }
}
