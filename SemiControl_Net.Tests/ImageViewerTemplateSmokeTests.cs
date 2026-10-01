// ImageViewerTemplateSmokeTests.cs
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Framework.Imaging;
using SemiControl.Controls;
using Xunit;

namespace SemiControl_Net.Tests
{
    /// <summary>
    /// 模板与帧管线的运行时冒烟：XAML 模板错误、PART_ 部件缺失、渲染管线断裂
    /// 只有真正实例化控件才暴露，纯逻辑单元测试盖不住，这里用真 STA 线程兜底。
    /// </summary>
    public class ImageViewerTemplateSmokeTests
    {
        [Fact]
        public void Template_Applies_And_FramePipeline_Runs()
        {
            Exception? failure = null;
            bool templateApplied = false;
            bool frameRendered = false;
            bool imageVisible = false;
            double observedZoom = 0;
            string diagnostic = string.Empty;

            var thread = new Thread(() =>
            {
                try
                {
                    var viewer = new ImageViewer();
                    // 测试进程没有 App 资源，手动合并主题字典，让隐式样式（含模板）生效。
                    viewer.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/SemiControl;component/Themes/Skin.xaml"),
                    });

                    var window = new Window
                    {
                        Content = viewer,
                        Width = 800,
                        Height = 600,
                        ShowActivated = false,
                        ShowInTaskbar = false,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = -3000, // 摆到屏幕外，不打扰任何人
                        Top = -3000,
                    };
                    window.Show();

                    var timer = new DispatcherTimer(DispatcherPriority.Background)
                    {
                        Interval = TimeSpan.FromMilliseconds(100),
                    };
                    int ticks = 0;
                    timer.Tick += (_, _) =>
                    {
                        try
                        {
                            ticks++;
                            if (ticks == 1)
                            {
                                var template = viewer.Template;
                                bool styleResolved = viewer.Style is not null;
                                var presenter = template?.FindName("PART_Presenter", viewer);
                                var infoZoom = template?.FindName("PART_InfoZoom", viewer);
                                // 注：ApplyTemplate() 在布局阶段已建好可视树时会返回 false（API 约定），
                                // 故以"样式解析 + 模板部件就位"为准。
                                templateApplied = styleResolved
                                    && template is not null
                                    && presenter is ImageViewerPresenter
                                    && infoZoom is TextBlock;
                                diagnostic = $"styleResolved={styleResolved}, "
                                    + $"template={(template is null ? "null" : "ok")}, "
                                    + $"presenter={presenter?.GetType().Name ?? "null"}, "
                                    + $"infoZoom={infoZoom?.GetType().Name ?? "null"}";

                                // 模拟泵线程：从非 UI 线程送帧（跨线程契约）
                                var feeder = new Thread(() => viewer.Show(
                                    new ImageFrame(4, 4, ImagePixelFormat.Mono8, new byte[16])));
                                feeder.Start();
                                feeder.Join();
                            }
                            else if (ticks >= 5)
                            {
                                // 首帧自动适应 800×600 视口：4×4 图像被放大到上限 16 倍 → 1600%
                                observedZoom = viewer.CurrentZoom;
                                frameRendered = Math.Abs(viewer.CurrentZoom - 1600) < 0.01;

                                // 像素级验证：真渲染一帧位图，中心区域应是全零帧（黑色），
                                // 抓"WritePixels 跑了但位图没挂到 presenter 上"这类纯数值断言盖不住的断裂。
                                var rtb = new RenderTargetBitmap(800, 600, 96, 96, PixelFormats.Pbgra32);
                                rtb.Render(window);
                                var center = new CroppedBitmap(rtb, new Int32Rect(350, 240, 100, 120));
                                var pixels = new byte[100 * 120 * 4];
                                center.CopyPixels(pixels, 100 * 4, 0);
                                bool darkPixelFound = false;
                                for (int i = 0; i + 3 < pixels.Length; i += 4)
                                {
                                    if (pixels[i + 3] == 255 && pixels[i] < 60 && pixels[i + 1] < 60 && pixels[i + 2] < 60)
                                    {
                                        darkPixelFound = true;
                                        break;
                                    }
                                }

                                imageVisible = darkPixelFound;

                                timer.Stop();
                                window.Close();
                                Dispatcher.CurrentDispatcher.BeginInvoke(
                                    DispatcherPriority.Send,
                                    new Action(() => Dispatcher.CurrentDispatcher.InvokeShutdown()));
                            }
                        }
                        catch (Exception ex)
                        {
                            failure = ex;
                            timer.Stop();
                            Dispatcher.CurrentDispatcher.InvokeShutdown();
                        }
                    };
                    timer.Start();
                    Dispatcher.Run();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(15));

            Assert.True(!thread.IsAlive, "STA 线程 15 秒内未退出，渲染管线疑似挂死。");
            Assert.Null(failure);
            Assert.True(templateApplied, $"模板未生效或 PART_ 部件缺失：{diagnostic}");
            Assert.True(frameRendered, $"帧管线未跑通（CurrentZoom={observedZoom}，期望首帧自动适应后的 1600%）。");
            Assert.True(imageVisible, "帧数据已处理但画面上没有像素——位图未挂到渲染表面（presenter.Bitmap）或 DrawImage 断裂。");
        }
    }
}
