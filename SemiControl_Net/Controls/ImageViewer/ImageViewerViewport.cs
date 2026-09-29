// Controls/ImageViewer/ImageViewerViewport.cs
using System;

namespace SemiControl.Controls
{
    /// <summary>
    /// 图像查看控件的视口数学：缩放、平移、图像坐标与屏幕坐标互转。
    /// 坐标映射约定：<c>screen = image × Scale − Offset</c>（Offset 为屏幕原点对应的图像偏移量，单位为换算后的屏幕像素）。
    /// 纯数学实现，不含 WPF 类型与渲染逻辑，便于单元测试。
    /// </summary>
    public sealed class ImageViewerViewport
    {
        /// <summary>创建视口。<paramref name="minScale"/> 与 <paramref name="maxScale"/> 会被互相校正为合法区间。</summary>
        public ImageViewerViewport(double minScale = 0.1, double maxScale = 16.0)
        {
            if (minScale <= 0)
            {
                throw new ArgumentException("最小缩放必须为正数。", nameof(minScale));
            }

            if (maxScale < minScale)
            {
                throw new ArgumentException("最大缩放不能小于最小缩放。", nameof(maxScale));
            }

            _minScale = minScale;
            _maxScale = maxScale;
        }

        /// <summary>允许的最小缩放倍率。</summary>
        public double MinScale => _minScale;

        /// <summary>允许的最大缩放倍率。</summary>
        public double MaxScale => _maxScale;

        private double _minScale;
        private double _maxScale;

        /// <summary>当前缩放倍率（1 = 图像像素与屏幕像素 1:1）。</summary>
        public double Scale { get; private set; } = 1.0;

        /// <summary>水平偏移（screen = image × Scale − Offset）。</summary>
        public double OffsetX { get; private set; }

        /// <summary>垂直偏移（screen = image × Scale − Offset）。</summary>
        public double OffsetY { get; private set; }

        /// <summary>把缩放倍率夹取到允许区间内。供外部（控件）在调整 MinZoom/MaxZoom 后复用。</summary>
        public double ClampScale(double scale) => Math.Clamp(scale, MinScale, MaxScale);

        /// <summary>
        /// 调整允许的缩放区间。防御式处理：参数不合法时按 min/max 互换与下限 1e-6 校正，不抛异常，
        /// 保证控件在宿主乱配 MinZoom/MaxZoom 时也不会在渲染路径上炸掉。
        /// </summary>
        public void SetScaleRange(double minScale, double maxScale)
        {
            if (minScale > maxScale)
            {
                (minScale, maxScale) = (maxScale, minScale);
            }

            minScale = Math.Max(minScale, 1e-6);
            _minScale = minScale;
            _maxScale = Math.Max(maxScale, minScale);
            Scale = ClampScale(Scale);
        }

        /// <summary>以屏幕点 (screenX, screenY) 为锚点缩放：缩放前后该屏幕点下的图像内容保持不动。</summary>
        public void ZoomAt(double screenX, double screenY, double zoomFactor)
        {
            if (zoomFactor <= 0)
            {
                throw new ArgumentException("缩放系数必须为正数。", nameof(zoomFactor));
            }

            double newScale = ClampScale(Scale * zoomFactor);
            if (Math.Abs(newScale - Scale) < double.Epsilon)
            {
                return;
            }

            // 锚点下的图像坐标：image = (screen + Offset) / Scale，缩放后保持不变。
            double imageX = (screenX + OffsetX) / Scale;
            double imageY = (screenY + OffsetY) / Scale;
            OffsetX = imageX * newScale - screenX;
            OffsetY = imageY * newScale - screenY;
            Scale = newScale;
        }

        /// <summary>平移：屏幕位移 (dx, dy) 直接作用于偏移量。</summary>
        public void Pan(double dx, double dy)
        {
            OffsetX -= dx;
            OffsetY -= dy;
        }

        /// <summary>适应窗口：整幅图像可见并居中（取宽高比中较小者，可放大亦可缩小）。</summary>
        public void FitTo(double imageWidth, double imageHeight, double viewWidth, double viewHeight)
        {
            if (imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0)
            {
                return;
            }

            double scale = ClampScale(Math.Min(viewWidth / imageWidth, viewHeight / imageHeight));
            SetScaleCentered(scale, imageWidth, imageHeight, viewWidth, viewHeight);
        }

        /// <summary>实际大小（1:1）：缩放复位为 1 并把图像居中。</summary>
        public void ActualSize(double imageWidth, double imageHeight, double viewWidth, double viewHeight)
        {
            if (imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0)
            {
                return;
            }

            SetScaleCentered(ClampScale(1.0), imageWidth, imageHeight, viewWidth, viewHeight);
        }

        /// <summary>屏幕坐标 → 图像坐标。</summary>
        public (double X, double Y) ScreenToImage(double screenX, double screenY)
            => ((screenX + OffsetX) / Scale, (screenY + OffsetY) / Scale);

        /// <summary>图像坐标 → 屏幕坐标。</summary>
        public (double X, double Y) ImageToScreen(double imageX, double imageY)
            => (imageX * Scale - OffsetX, imageY * Scale - OffsetY);

        private void SetScaleCentered(double scale, double imageWidth, double imageHeight, double viewWidth, double viewHeight)
        {
            Scale = scale;
            OffsetX = (imageWidth * scale - viewWidth) / 2;
            OffsetY = (imageHeight * scale - viewHeight) / 2;
        }
    }
}
