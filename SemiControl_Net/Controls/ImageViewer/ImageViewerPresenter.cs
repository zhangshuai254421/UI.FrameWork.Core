// Controls/ImageViewer/ImageViewerPresenter.cs
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Framework.Imaging;

namespace SemiControl.Controls
{
    /// <summary>
    /// 图像查看控件的绘制表面（模板部件 PART_Presenter）：在 <see cref="OnRender"/> 里
    /// 先画 WriteableBitmap 帧，再委托宿主控件画矢量层（十字线/ROI/标注/检测结果）。
    /// 自身无状态、无交互——鼠标键盘事件冒泡到宿主 <see cref="ImageViewer"/> 处理。
    /// </summary>
    public sealed class ImageViewerPresenter : FrameworkElement
    {
        /// <summary>宿主控件，由 <see cref="ImageViewer.OnApplyTemplate"/> 回填。</summary>
        internal ImageViewer? Owner { get; set; }

        /// <summary>当前帧位图（Bgra32），由宿主控件生成并更新；null 表示尚无帧。</summary>
        internal WriteableBitmap? Bitmap { get; set; }

        /// <summary>公开默认构造仅为满足模板 XAML 对 PART_ 部件的命名要求；本类型仅供 <see cref="ImageViewer"/> 模板使用。</summary>
        public ImageViewerPresenter()
        {
            // 像素级放大查看需要锯齿可见（点阵感），同时省掉双三次重采样的开销。
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            ClipToBounds = true;
        }

        /// <summary>画布尺寸即视口尺寸；无限可用空间时退化为零（避免 Arrange 阶段抛异常）。</summary>
        protected override Size MeasureOverride(Size availableSize)
            => double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height)
                ? default
                : availableSize;

        protected override void OnRender(DrawingContext drawingContext)
        {
            var bitmap = Bitmap;
            if (bitmap is not null)
            {
                // screen = image × Scale − Offset，位图左上角即图像 (0,0) 的屏幕位置。
                var owner = Owner;
                var viewport = owner?.Viewport ?? new ImageViewerViewport();
                double scale = viewport.Scale;
                (double x, double y) = viewport.ImageToScreen(0, 0);
                drawingContext.DrawImage(bitmap, new Rect(x, y, bitmap.PixelWidth * scale, bitmap.PixelHeight * scale));
            }

            Owner?.RenderVectorLayer(drawingContext, this);
        }
    }
}
