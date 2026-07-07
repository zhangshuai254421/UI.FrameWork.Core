using Framework.Core.Common;
using Serilog;
using Serilog.Sinks.RichTextBox.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace UI.FrameWork.Core.Main.Footer
{
    
/// <summary>
/// LogShowView.xaml 的交互逻辑
/// </summary>
public partial class LogShowView : UserControl
    {
        public static RichTextBoxConsoleTheme ColoredCustom { get; } = new RichTextBoxConsoleTheme(new Dictionary<RichTextBoxThemeStyle, RichTextBoxConsoleThemeStyle>
        {
            // 普通文本 - 深灰，保证可读性
            [RichTextBoxThemeStyle.Text] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#1a1a1a"  // 接近黑色但柔和
            },

            // 次要文本（时间戳、元数据）- 中灰色，降低视觉权重
            [RichTextBoxThemeStyle.SecondaryText] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#555555"
            },

            // 三级文本 - 更淡的灰色
            [RichTextBoxThemeStyle.TertiaryText] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#777777"
            },

            // 无效/错误标记 - 亮红色，醒目但不刺眼
            [RichTextBoxThemeStyle.Invalid] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#d32f2f",  // 深红
            },

            // null 关键字 - 紫色，与普通文本区分
            [RichTextBoxThemeStyle.Null] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#7b1fa2"   // 紫色
            },

            // 属性名 - 深蓝绿色
            [RichTextBoxThemeStyle.Name] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#00695c"   // 墨绿/蓝绿色
            },

            // 字符串 - 深绿色（经典配色）
            [RichTextBoxThemeStyle.String] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#2e7d32"   // 森林绿
            },

            // 数字 - 橙棕色
            [RichTextBoxThemeStyle.Number] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#bf360c"   // 橙红/铁锈色
            },

            // 布尔值 - 深紫色
            [RichTextBoxThemeStyle.Boolean] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#4a148c",  // 深紫
            },

            // 标量值 - 深青色
            [RichTextBoxThemeStyle.Scalar] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#00838f"   // 深青
            },

            // Verbose 级别 - 淡灰色背景 + 深灰字
            [RichTextBoxThemeStyle.LevelVerbose] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#333333",
                Background = "#d0d0d0"   // 浅灰
            },

            // Debug 级别 - 浅青色背景
            [RichTextBoxThemeStyle.LevelDebug] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#0d3b4f",
                Background = "#b2ebf2"   // 淡青色
            },

            // Information 级别 - 浅蓝色背景（比你的背景色深一点）
            [RichTextBoxThemeStyle.LevelInformation] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#0d3b4f",  // 深蓝黑
                Background = "#90caf9"   // 柔和蓝色
            },

            // Warning 级别 - 琥珀/橙色背景 + 深色字
            [RichTextBoxThemeStyle.LevelWarning] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#1a1a1a",
                Background = "#ffb74d"   // 橙色
            },

            // Error 级别 - 浅红色背景 + 深色字
            [RichTextBoxThemeStyle.LevelError] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#1a1a1a",
                Background = "#ef9a9a"   // 浅红色
            },

            // Fatal 级别 - 深红色背景 + 白色字（最严重）
            [RichTextBoxThemeStyle.LevelFatal] = new RichTextBoxConsoleThemeStyle
            {
                Foreground = "#ffffff",
                Background = "#c62828",  // 深红色
            }
        });

        /// <summary>
        /// 日志行数上限，超过时自动清理旧日志
        /// </summary>
        private const int MaxLogLines = 5000;

        /// <summary>
        /// 滚动到底部的容差（像素）
        /// </summary>
        private const double ScrollTolerance = 20.0;

        private bool _isAutoScroll = true;

        public LogShowView()
        {
            InitializeComponent();

            // 自动滚动 + 行数限制
            LogRichTextBox.TextChanged += OnLogTextChanged;
            LogScrollViewer.ScrollChanged += OnScrollChanged;

            // 1. 初始化 Serilog（自定义浅色背景 + 黑色文字主题）


            Serilog.Log.Logger = new LoggerConfiguration()
                        // .Enrich.WithMachineName()             // 添加 MachineName
                        // .Enrich.WithThreadId()                // 添加 ThreadId
                        // .Enrich.WithEnvironmentUserName()     // 添加当前用户名
                        //.MinimumLevel.Debug()
                        //.WriteTo.SQLite(AppGlobals.LogDbFilePathNoDebug, tableName: "SerilogHistory")  // 指定数据库文件路径
                        //.WriteTo.File(
                        //    "logs/log-.txt",
                        //    rollingInterval: RollingInterval.Day,   // 按天分文件
                        //    retainedFileCountLimit: 10,            // 保留10天
                        //    outputTemplate:
                        //    "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                        //)
                        .WriteTo.RichTextBox(this.LogRichTextBox, theme: ColoredCustom)
                        .CreateLogger();
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 用户手动滚动：如果距离底部超过容差，暂停自动滚动
            if (e.ExtentHeightChange == 0)
            {
                _isAutoScroll = (e.VerticalOffset + e.ViewportHeight) >= (e.ExtentHeight - ScrollTolerance);
            }
        }

        private void OnLogTextChanged(object sender, TextChangedEventArgs e)
        {
            var richTextBox = (RichTextBox)sender;

            // 仅在用户未手动滚动时自动滚动到底部
            if (_isAutoScroll)
            {
                LogScrollViewer.ScrollToEnd();
            }

            // 限制行数，防止内存无限增长
            var document = richTextBox.Document;
            var lineCount = document.Blocks.Count;
            if (lineCount > MaxLogLines)
            {
                var blocksToRemove = lineCount - MaxLogLines;
                for (int i = 0; i < blocksToRemove; i++)
                {
                    var first = document.Blocks.FirstBlock;
                    if (first != null)
                        document.Blocks.Remove(first);
                }
            }
        }
    }
}
