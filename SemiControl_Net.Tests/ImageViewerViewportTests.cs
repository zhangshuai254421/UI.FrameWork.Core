// ImageViewerViewportTests.cs
using SemiControl.Controls;
using Xunit;

namespace SemiControl_Net.Tests
{
    /// <summary>视口数学契约：锚点缩放不动、适应居中、夹取与坐标互转。</summary>
    public class ImageViewerViewportTests
    {
        [Fact]
        public void InitialState_IsIdentity()
        {
            var viewport = new ImageViewerViewport();

            Assert.Equal(1.0, viewport.Scale);
            Assert.Equal(0.0, viewport.OffsetX);
            Assert.Equal(0.0, viewport.OffsetY);
        }

        [Fact]
        public void ZoomAt_KeepsAnchorImagePointFixed()
        {
            var viewport = new ImageViewerViewport();
            viewport.Pan(100, 50); // 画面先挪到某处

            double screenX = 300, screenY = 200;
            var before = viewport.ScreenToImage(screenX, screenY);

            viewport.ZoomAt(screenX, screenY, 2.0);

            Assert.Equal(2.0, viewport.Scale);
            var after = viewport.ScreenToImage(screenX, screenY);
            Assert.Equal(before.X, after.X, 9);
            Assert.Equal(before.Y, after.Y, 9);
        }

        [Fact]
        public void ZoomAt_ClampsToMaxScale()
        {
            var viewport = new ImageViewerViewport();
            for (int i = 0; i < 20; i++)
            {
                viewport.ZoomAt(0, 0, 2.0);
            }

            Assert.Equal(16.0, viewport.Scale);
        }

        [Fact]
        public void ZoomAt_ClampsToMinScale()
        {
            var viewport = new ImageViewerViewport();
            for (int i = 0; i < 20; i++)
            {
                viewport.ZoomAt(0, 0, 0.5);
            }

            Assert.Equal(0.1, viewport.Scale);
        }

        [Fact]
        public void ScreenToImage_And_ImageToScreen_AreInverse()
        {
            var viewport = new ImageViewerViewport();
            viewport.ZoomAt(10, 10, 3.0);
            viewport.Pan(-40, 25);

            var (ix, iy) = viewport.ScreenToImage(123.5, 77.25);
            var (sx, sy) = viewport.ImageToScreen(ix, iy);

            Assert.Equal(123.5, sx, 9);
            Assert.Equal(77.25, sy, 9);
        }

        [Fact]
        public void FitTo_UsesSmallerRatio_AndCenters()
        {
            var viewport = new ImageViewerViewport();
            // 图像 1000×500，视口 500×500：宽比 0.5、高比 1.0 → 取 0.5，整幅 250 高居中。
            viewport.FitTo(1000, 500, 500, 500);

            Assert.Equal(0.5, viewport.Scale);
            Assert.Equal(0.0, viewport.OffsetX);        // 宽度方向恰好铺满
            Assert.Equal((500 * 0.5 - 500) / 2, viewport.OffsetY); // 高度方向居中

            // 图像四个角都可见
            var topLeft = viewport.ImageToScreen(0, 0);
            var bottomRight = viewport.ImageToScreen(1000, 500);
            Assert.Equal(0, topLeft.X, 9);
            Assert.InRange(bottomRight.Y, 0, 500);
        }

        [Fact]
        public void FitTo_ClampsToScaleRange()
        {
            var viewport = new ImageViewerViewport(minScale: 0.1, maxScale: 4.0);
            // 100×100 图像放进 1000×1000 视口：天然比 10 倍 → 夹到 4。
            viewport.FitTo(100, 100, 1000, 1000);

            Assert.Equal(4.0, viewport.Scale);
        }

        [Fact]
        public void ActualSize_ResetsScaleToOne_AndCenters()
        {
            var viewport = new ImageViewerViewport();
            viewport.ZoomAt(0, 0, 8.0);
            viewport.ActualSize(2000, 1000, 400, 400);

            Assert.Equal(1.0, viewport.Scale);
            Assert.Equal((2000 - 400) / 2.0, viewport.OffsetX, 9);
            Assert.Equal((1000 - 400) / 2.0, viewport.OffsetY, 9);
        }

        [Fact]
        public void Pan_MovesOffsetByScreenDelta()
        {
            var viewport = new ImageViewerViewport();
            viewport.Pan(30, -20);

            Assert.Equal(-30, viewport.OffsetX);
            Assert.Equal(20, viewport.OffsetY);
        }

        [Fact]
        public void SetScaleRange_CorrectsInvertedBounds_AndClampsCurrentScale()
        {
            var viewport = new ImageViewerViewport();
            viewport.ZoomAt(0, 0, 16.0); // 顶到默认上限 16

            viewport.SetScaleRange(minScale: 8.0, maxScale: 2.0); // 故意写反

            Assert.True(viewport.MinScale <= viewport.MaxScale);
            Assert.Equal(viewport.MaxScale, viewport.Scale); // 16 被夹到新的上限
        }

        [Fact]
        public void FitTo_WithNonPositiveSizes_IsNoOp()
        {
            var viewport = new ImageViewerViewport();
            viewport.FitTo(0, 100, 500, 500);
            viewport.FitTo(100, 0, 500, 500);
            viewport.FitTo(100, 100, 0, 500);

            Assert.Equal(1.0, viewport.Scale);
            Assert.Equal(0.0, viewport.OffsetX);
        }
    }
}
