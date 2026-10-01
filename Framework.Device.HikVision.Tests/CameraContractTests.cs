using Framework.Device.Domain;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// ICamera 契约测试：用假适配器验证真实契约行为（缝①）。
    /// </summary>
    public class CameraContractTests
    {
        /// <summary>用 TCP 连接参数打开假相机，预期打开成功且记录的连接对象与传入的是同一实例。</summary>
        [Fact]
        public void 打开时记录连接方式()
        {
            var camera = new FakeCamera();
            var tcp = new TcpConnection("192.168.1.64", 8000);

            Assert.True(camera.Open(tcp));
            Assert.Same(tcp, camera.OpenedConnection);
            Assert.True(camera.IsOpen);
        }

        /// <summary>设置曝光与增益后读回，预期读到的值与设置值完全一致。</summary>
        [Fact]
        public void 曝光与增益可强类型设置并读回()
        {
            var camera = new FakeCamera();

            camera.SetExposureTime(10000.5);
            camera.SetGain(3.2);

            Assert.Equal(10000.5, camera.GetExposureTime());
            Assert.Equal(3.2, camera.GetGain());
        }

        /// <summary>预置一帧图像后取帧，预期返回托管字节数组且宽、高、像素格式与预置值一致。</summary>
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

        /// <summary>向实时图像通道写入一帧并异步读取，预期收到的就是刚写入的那一帧。</summary>
        [Fact]
        public async Task 实时图像通道能收到帧()
        {
            var camera = new FakeCamera();
            var frame = new CameraData { ImageData = new byte[] { 9 }, Width = 1, Height = 1 };

            camera.ChannelCameraData.Writer.TryWrite(frame);
            var read = await camera.ChannelCameraData.Reader.ReadAsync();

            Assert.Same(frame, read);
        }

        /// <summary>连续触发两次软触发，预期触发计数递增到 2。</summary>
        [Fact]
        public void 软触发计数递增()
        {
            var camera = new FakeCamera();

            camera.TriggerSoftware();
            camera.TriggerSoftware();

            Assert.Equal(2, camera.TriggerCount);
        }

        /// <summary>先开始再停止采集，预期取流状态标志相应地在真与假之间切换。</summary>
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
