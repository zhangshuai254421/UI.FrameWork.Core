// ImageViewerDemoWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Threading;
using SemiControl.Controls;

namespace SemiControlDemo_Net
{
    /// <summary>
    /// ImageViewer 控件演示窗口 — 合成帧模拟相机流，演示缩放/平移/标注/ROI/检测结果叠加。
    /// 不依赖 Framework.Device：图像帧直接合成，与"控件零设备依赖"的设计呼应。
    /// </summary>
    public partial class ImageViewerDemoWindow : Window
    {
        // 合成图像规格（对齐预览档：百万像素级、30fps）
        private const int ImageWidth = 1280;
        private const int ImageHeight = 1024;
        private const double Fps = 30;

        private readonly DispatcherTimer _timer;
        private readonly byte[] _background = new byte[ImageWidth * ImageHeight]; // 静态底图（渐变+噪声），只算一次
        private readonly byte[] _frameBuffer = new byte[ImageWidth * ImageHeight];
        private double _phase; // 移动光斑的相位

        public ImageViewerDemoWindow()
        {
            InitializeComponent();

            BuildBackground();

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(1000 / Fps),
            };
            _timer.Tick += (_, _) => PushSyntheticFrame();

            viewer.RoiChanged += (_, roi) =>
                txtRoi.Text = roi.HasArea
                    ? $"X={roi.X}, Y={roi.Y}, W={roi.Width}, H={roi.Height}"
                    : "无";

            // 打开窗口即出图：避免空控件一片白、按钮点了没反应的困惑。
            Loaded += (_, _) =>
            {
                if (!_timer.IsEnabled)
                {
                    _timer.Start();
                    btnStream.Content = "⏸ 停止模拟流";
                }
            };

            Closed += (_, _) => _timer.Stop();
        }

        private void BtnStream_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                btnStream.Content = "▶ 开始模拟流（1280×1024 @30fps）";
            }
            else
            {
                _timer.Start();
                btnStream.Content = "⏸ 停止模拟流";
            }
        }

        private void BtnInjectResults_Click(object sender, RoutedEventArgs e)
        {
            // 模拟宿主随检测周期整体替换的检测结果：轮廓 + 文字标签（图像像素坐标）。
            var scratch = ViewerShape.Rectangle(420, 300, 180, 120);
            scratch.Label = "划痕 0.92";
            var chipOff = ViewerShape.Ellipse(760, 520, 150, 150);
            chipOff.Label = "缺角 0.87";
            viewer.Results = new System.Collections.ObjectModel.ObservableCollection<ViewerShape> { scratch, chipOff };
        }

        private void BtnClearAnnotations_Click(object sender, RoutedEventArgs e) => viewer.ClearAnnotations();

        private void BtnClearRoi_Click(object sender, RoutedEventArgs e) => viewer.ClearRoi();

        private void BtnFit_Click(object sender, RoutedEventArgs e) => viewer.FitToWindow();

        private void BtnActualSize_Click(object sender, RoutedEventArgs e) => viewer.ActualSize();

        private void ChkCrosshair_Changed(object sender, RoutedEventArgs e)
        {
            if (viewer != null)
            {
                viewer.IsCenterCrosshair = chkCrosshair.IsChecked == true;
            }
        }

        /// <summary>
        /// 预生成静态底图：深灰底 + 128px 网格 + 高对比固定特征（亮白块/纯黑洞/白圆环/四角基准十字）。
        /// 特征都是肉眼一眼可辨的强对比结构，方便直观判断缩放、平移、标注是否正确。只算一次。
        /// </summary>
        private void BuildBackground()
        {
            var rand = new Random(7);
            for (int y = 0; y < ImageHeight; y++)
            {
                int row = y * ImageWidth;
                bool gridRow = y % 128 == 0;
                for (int x = 0; x < ImageWidth; x++)
                {
                    byte value = (byte)(45 + rand.Next(-3, 4));
                    if (gridRow || x % 128 == 0)
                    {
                        value = 96;
                    }

                    _background[row + x] = value;
                }
            }

            FillRect(_background, 900, 140, 260, 180, 235);  // 亮白块
            FillDisc(_background, 300, 260, 100, 8);         // 纯黑洞
            FillRing(_background, 640, 780, 120, 70, 230);   // 白色圆环
            DrawCross(_background, 64, 64, 24, 255);         // 四角基准十字
            DrawCross(_background, 1216, 64, 24, 255);
            DrawCross(_background, 64, 960, 24, 255);
            DrawCross(_background, 1216, 960, 24, 255);
        }

        /// <summary>生成一帧：底图拷贝 + 一颗带黑边的亮斑（黑边保证亮斑压在白块上也可见），推给控件。</summary>
        private void PushSyntheticFrame()
        {
            Array.Copy(_background, _frameBuffer, _frameBuffer.Length);

            _phase += 0.05;
            double cx = ImageWidth / 2.0 + Math.Cos(_phase) * ImageWidth * 0.3;
            double cy = ImageHeight / 2.0 + Math.Sin(_phase * 0.7) * ImageHeight * 0.3;
            DrawDisc(_frameBuffer, cx, cy, radius: 66, brightness: 10);  // 黑色描边
            DrawDisc(_frameBuffer, cx, cy, radius: 58, brightness: 255); // 亮斑本体

            viewer.Show(new ImageFrame(ImageWidth, ImageHeight, ImagePixelFormat.Mono8, _frameBuffer));
        }

        private void FillRect(byte[] target, int x, int y, int w, int h, byte value)
        {
            int x1 = Math.Min(ImageWidth, x + w);
            int y1 = Math.Min(ImageHeight, y + h);
            for (int yy = Math.Max(0, y); yy < y1; yy++)
            {
                int row = yy * ImageWidth;
                for (int xx = Math.Max(0, x); xx < x1; xx++)
                {
                    target[row + xx] = value;
                }
            }
        }

        private void FillDisc(byte[] target, double centerX, double centerY, double radius, byte value)
        {
            for (int y = ClampY(centerY - radius), y1 = ClampY(centerY + radius); y <= y1; y++)
            {
                double dy = y - centerY;
                int row = y * ImageWidth;
                for (int x = ClampX(centerX - radius), x1 = ClampX(centerX + radius); x <= x1; x++)
                {
                    double dx = x - centerX;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        target[row + x] = value;
                    }
                }
            }
        }

        private void FillRing(byte[] target, double centerX, double centerY, double outerRadius, double innerRadius, byte value)
        {
            double inner2 = innerRadius * innerRadius;
            for (int y = ClampY(centerY - outerRadius), y1 = ClampY(centerY + outerRadius); y <= y1; y++)
            {
                double dy = y - centerY;
                int row = y * ImageWidth;
                for (int x = ClampX(centerX - outerRadius), x1 = ClampX(centerX + outerRadius); x <= x1; x++)
                {
                    double dx = x - centerX;
                    double d2 = dx * dx + dy * dy;
                    if (d2 <= outerRadius * outerRadius && d2 >= inner2)
                    {
                        target[row + x] = value;
                    }
                }
            }
        }

        private void DrawCross(byte[] target, int centerX, int centerY, int arm, byte value)
        {
            for (int i = -arm; i <= arm; i++)
            {
                target[ClampY(centerY) * ImageWidth + ClampX(centerX + i)] = value;
                target[ClampY(centerY + i) * ImageWidth + ClampX(centerX)] = value;
            }
        }

        private int ClampX(double v) => Math.Clamp((int)Math.Round(v), 0, ImageWidth - 1);

        private int ClampY(double v) => Math.Clamp((int)Math.Round(v), 0, ImageHeight - 1);

        /// <summary>移动亮斑用：边缘 6 像素线性羽化，避免硬边闪烁。</summary>
        private void DrawDisc(byte[] target, double centerX, double centerY, double radius, byte brightness)
        {
            int x0 = Math.Max(0, (int)(centerX - radius));
            int x1 = Math.Min(ImageWidth - 1, (int)(centerX + radius));
            int y0 = Math.Max(0, (int)(centerY - radius));
            int y1 = Math.Min(ImageHeight - 1, (int)(centerY + radius));
            double r2 = radius * radius;

            for (int y = y0; y <= y1; y++)
            {
                double dy = y - centerY;
                int row = y * ImageWidth;
                for (int x = x0; x <= x1; x++)
                {
                    double dx = x - centerX;
                    double d2 = dx * dx + dy * dy;
                    if (d2 <= r2)
                    {
                        double edge = Math.Min(1.0, (radius - Math.Sqrt(d2)) / 6.0);
                        target[row + x] = (byte)(brightness * edge);
                    }
                }
            }
        }
    }
}
