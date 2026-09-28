using Framework.Device;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// CameraBase 契约测试：预览通道行为与取流模式默认值（不依赖任何硬件）。
    /// </summary>
    public class CameraBaseTests
    {
        [Fact]
        public void 预览通道_容量3_满时丢最旧帧()
        {
            var camera = new FakeCamera();

            for (byte i = 1; i <= 5; i++)
            {
                Assert.True(camera.ChannelCameraData.Writer.TryWrite(new CameraData { ImageData = new[] { i } }));
            }

            Assert.Equal(3, camera.ChannelCameraData.Reader.Count);
            Assert.True(camera.ChannelCameraData.Reader.TryRead(out var first));
            Assert.Equal(3, first.ImageData[0]); // 1、2 已被丢弃
            Assert.True(camera.ChannelCameraData.Reader.TryRead(out var second));
            Assert.Equal(4, second.ImageData[0]);
            Assert.True(camera.ChannelCameraData.Reader.TryRead(out var third));
            Assert.Equal(5, third.ImageData[0]);
        }

        [Fact]
        public void 取流模式_默认主动拉帧()
        {
            Assert.Equal(CameraGrabMode.Pull, new FakeCamera().GrabMode);
        }
    }
}
