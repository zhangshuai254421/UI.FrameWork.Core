using System;
using System.Windows;

namespace SemiControlDemo_Net
{
    /// <summary>
    /// Demo 入口：支持命令行直启各个演示窗口，便于单独调试某个控件。
    /// 无参数 → DataGrid 主窗口；"imageviewer" → ImageViewer 演示窗口。
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Window window = e.Args is { Length: > 0 }
                && string.Equals(e.Args[0], "imageviewer", StringComparison.OrdinalIgnoreCase)
                    ? new ImageViewerDemoWindow()
                    : new MainWindow();

            window.Show();
        }
    }
}
