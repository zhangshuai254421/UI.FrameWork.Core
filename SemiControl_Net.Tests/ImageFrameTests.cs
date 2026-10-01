// ImageFrameTests.cs
using Framework.Imaging;
using SemiControl.Controls;
using Xunit;

namespace SemiControl_Net.Tests
{
    /// <summary>ImageFrame 构造校验契约：非法参数必须被拒之门外，渲染层不做二次防御。</summary>
    public class ImageFrameTests
    {
        /// <summary>用合法的 Mono8 参数构造 ImageFrame，预期宽、高、格式与数据长度原样保留。</summary>
        [Fact]
        public void Constructor_WithValidMono8Frame_Accepts()
        {
            var frame = new ImageFrame(2, 2, ImagePixelFormat.Mono8, new byte[4]);

            Assert.Equal(2, frame.Width);
            Assert.Equal(2, frame.Height);
            Assert.Equal(ImagePixelFormat.Mono8, frame.PixelFormat);
            Assert.Equal(4, frame.Data.Length);
        }

        /// <summary>宽或高为零或负数时构造，预期抛出 ArgumentException。</summary>
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

        /// <summary>像素数据为 null 时构造，预期抛出 ArgumentNullException。</summary>
        [Fact]
        public void Constructor_WithNullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ImageFrame(2, 2, ImagePixelFormat.Mono8, null!));
        }

        /// <summary>数据长度比该格式一帧所需字节数少一时构造，预期抛出 ArgumentException；各格式所需字节数随用例给出。</summary>
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

        /// <summary>数据长度超过一帧所需字节数时构造，预期接受且保留完整缓冲长度。</summary>
        [Fact]
        public void Constructor_WithExtraData_Accepts()
        {
            // 允许富余（泵方可能整块复用池化缓冲），只需不少于所需。
            var frame = new ImageFrame(2, 2, ImagePixelFormat.Mono8, new byte[100]);
            Assert.Equal(100, frame.Data.Length);
        }
    }
}
