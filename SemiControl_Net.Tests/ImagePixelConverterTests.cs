// ImagePixelConverterTests.cs
using Framework.Imaging;
using SemiControl.Controls;
using Xunit;

namespace SemiControl_Net.Tests
{
    /// <summary>像素格式转换契约：Bgra32 输出、Mono16 高字节有效、越界与数据不足走 false 而非异常。</summary>
    public class ImagePixelConverterTests
    {
        /// <summary>各像素格式的每像素字节数应与约定值一致：Mono8 为 1、Mono16 为 2、RGB8 与 BGR8 为 3。</summary>
        [Theory]
        [InlineData(ImagePixelFormat.Mono8, 1)]
        [InlineData(ImagePixelFormat.Mono16, 2)]
        [InlineData(ImagePixelFormat.RGB8, 3)]
        [InlineData(ImagePixelFormat.BGR8, 3)]
        public void GetBytesPerPixel_MatchesFormat(ImagePixelFormat format, int expected)
            => Assert.Equal(expected, ImagePixelConverter.GetBytesPerPixel(format));

        /// <summary>Mono8 转 Bgra32：灰度值应复制到 B、G、R 三个通道，Alpha 恒为 255。</summary>
        [Fact]
        public void TryConvertToBgra32_Mono8_ReplicatesGrayIntoBGR()
        {
            var frame = new ImageFrame(2, 1, ImagePixelFormat.Mono8, new byte[] { 0x00, 0xFF });
            var output = new byte[2 * 1 * 4];

            Assert.True(ImagePixelConverter.TryConvertToBgra32(frame, output));

            // 像素 0：黑；像素 1：白。B-G-R 同值，A 恒 255。
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, output);
        }

        /// <summary>Mono16 转 Bgra32：按小端容器取高字节为有效灰度并复制到 B、G、R。</summary>
        [Fact]
        public void TryConvertToBgra32_Mono16_TakesHighByte()
        {
            // 小端容器：字节 0 为低位、字节 1 为高位有效（与 Framework.Device 落盘约定一致）。
            var frame = new ImageFrame(1, 1, ImagePixelFormat.Mono16, new byte[] { 0xAB, 0x12 });
            var output = new byte[4];

            Assert.True(ImagePixelConverter.TryConvertToBgra32(frame, output));
            Assert.Equal(0x12, output[0]); // B
            Assert.Equal(0x12, output[1]); // G
            Assert.Equal(0x12, output[2]); // R
            Assert.Equal(0xFF, output[3]);
        }

        /// <summary>RGB8 转 Bgra32：输出通道序应翻转为 BGR。</summary>
        [Fact]
        public void TryConvertToBgra32_RGB8_ReordersToBGR()
        {
            var frame = new ImageFrame(1, 1, ImagePixelFormat.RGB8, new byte[] { 0x11, 0x22, 0x33 });
            var output = new byte[4];

            Assert.True(ImagePixelConverter.TryConvertToBgra32(frame, output));
            Assert.Equal(0x33, output[0]); // B ← 源第 3 字节
            Assert.Equal(0x22, output[1]); // G
            Assert.Equal(0x11, output[2]); // R ← 源第 1 字节
        }

        /// <summary>BGR8 转 Bgra32：输出通道序应保持不变。</summary>
        [Fact]
        public void TryConvertToBgra32_BGR8_KeepsOrder()
        {
            var frame = new ImageFrame(1, 1, ImagePixelFormat.BGR8, new byte[] { 0x11, 0x22, 0x33 });
            var output = new byte[4];

            Assert.True(ImagePixelConverter.TryConvertToBgra32(frame, output));
            Assert.Equal(0x11, output[0]); // B
            Assert.Equal(0x22, output[1]); // G
            Assert.Equal(0x33, output[2]); // R
        }

        /// <summary>帧为 null 时转换，预期返回 false 而不抛异常。</summary>
        [Fact]
        public void TryConvertToBgra32_WithNullFrame_ReturnsFalse()
        {
            Assert.False(ImagePixelConverter.TryConvertToBgra32(null, new byte[4]));
        }

        /// <summary>输出缓冲比所需少一字节时转换，预期返回 false 而不抛异常。</summary>
        [Fact]
        public void TryConvertToBgra32_WithSmallDestination_ReturnsFalse()
        {
            var frame = new ImageFrame(2, 2, ImagePixelFormat.Mono8, new byte[4]);
            Assert.False(ImagePixelConverter.TryConvertToBgra32(frame, new byte[4 * 4 - 1]));
        }

        /// <summary>Mono8 按坐标取显示色，预期返回该坐标像素的灰度值且三通道相同。</summary>
        [Fact]
        public void TryGetDisplayColor_Mono8_ReturnsGrayAtCoordinate()
        {
            var frame = new ImageFrame(2, 2, ImagePixelFormat.Mono8, new byte[] { 1, 2, 3, 4 });

            Assert.True(ImagePixelConverter.TryGetDisplayColor(frame, 1, 0, out byte r, out byte g, out byte b));
            Assert.Equal(2, r);
            Assert.Equal(2, g);
            Assert.Equal(2, b);
        }

        /// <summary>BGR8 按坐标取显示色，预期通道序还原为 R、G、B。</summary>
        [Fact]
        public void TryGetDisplayColor_BGR8_UnswapsChannels()
        {
            var frame = new ImageFrame(1, 1, ImagePixelFormat.BGR8, new byte[] { 0x10, 0x20, 0x30 });

            Assert.True(ImagePixelConverter.TryGetDisplayColor(frame, 0, 0, out byte r, out byte g, out byte b));
            Assert.Equal(0x30, r);
            Assert.Equal(0x20, g);
            Assert.Equal(0x10, b);
        }

        /// <summary>坐标越界（负值或超出宽高）时取显示色，预期返回 false。</summary>
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(2, 0)]
        [InlineData(0, 2)]
        public void TryGetDisplayColor_OutOfRange_ReturnsFalse(int x, int y)
        {
            var frame = new ImageFrame(2, 2, ImagePixelFormat.Mono8, new byte[4]);
            Assert.False(ImagePixelConverter.TryGetDisplayColor(frame, x, y, out _, out _, out _));
        }

        /// <summary>帧为 null 时取显示色，预期返回 false 而不抛异常。</summary>
        [Fact]
        public void TryGetDisplayColor_WithNullFrame_ReturnsFalse()
            => Assert.False(ImagePixelConverter.TryGetDisplayColor(null, 0, 0, out _, out _, out _));
    }
}
