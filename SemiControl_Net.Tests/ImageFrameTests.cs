// ImageFrameTests.cs
using SemiControl.Controls;
using Xunit;

namespace SemiControl_Net.Tests
{
    /// <summary>ImageFrame 构造校验契约：非法参数必须被拒之门外，渲染层不做二次防御。</summary>
    public class ImageFrameTests
    {
        [Fact]
        public void Constructor_WithValidMono8Frame_Accepts()
        {
            var frame = new ImageFrame(2, 2, ImagePixelFormat.Mono8, new byte[4]);

            Assert.Equal(2, frame.Width);
            Assert.Equal(2, frame.Height);
            Assert.Equal(ImagePixelFormat.Mono8, frame.PixelFormat);
            Assert.Equal(4, frame.Data.Length);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(-1, 2)]
        [InlineData(2, 0)]
        [InlineData(2, -3)]
        public void Constructor_WithNonPositiveSize_Throws(int width, int height)
        {
            Assert.Throws<ArgumentException>(
                () => new ImageFrame(width, height, ImagePixelFormat.Mono8, new byte[16]));
        }

        [Fact]
        public void Constructor_WithNullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ImageFrame(2, 2, ImagePixelFormat.Mono8, null!));
        }

        [Theory]
        [InlineData(ImagePixelFormat.Mono8, 4)]   // 2×2×1
        [InlineData(ImagePixelFormat.Mono16, 8)]  // 2×2×2
        [InlineData(ImagePixelFormat.RGB8, 12)]   // 2×2×3
        [InlineData(ImagePixelFormat.BGR8, 12)]   // 2×2×3
        public void Constructor_WithInsufficientData_Throws(ImagePixelFormat format, int required)
        {
            Assert.Throws<ArgumentException>(
                () => new ImageFrame(2, 2, format, new byte[required - 1]));
        }

        [Fact]
        public void Constructor_WithExtraData_Accepts()
        {
            // 允许富余（泵方可能整块复用池化缓冲），只需不少于所需。
            var frame = new ImageFrame(2, 2, ImagePixelFormat.Mono8, new byte[100]);
            Assert.Equal(100, frame.Data.Length);
        }
    }
}
