using Framework.Core.Common;
using Microsoft.Extensions.Logging;
using PrismUI.Core;
using Serilog;
using Serilog.Sinks.RichTextBox.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main.Footer
{
    /// <summary>日志展示页 ViewModel：进入页面后后台持续产出各级别测试日志，离开页面时停止。</summary>
    public class LogShowViewModel : BaseViewModel, INavigationAware
    {
        #region 构造函数

        /// <summary>空构造：仅沿用 BaseViewModel 的默认初始化。</summary>
        public LogShowViewModel() // ← 多传一个参数，其余不变
        {
        }

        #endregion

        #region 属性

        /// <summary>后台日志任务的取消源；进入页面时重建，离开页面时取消。</summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        #endregion

        #region INavigationAware

        /// <summary>始终复用当前视图实例作为导航目标，避免重复创建导致 Serilog sink 丢失。</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;  // 复用同一个 View 实例，避免重复创建导致 Serilog sink 丢失
        }

        /// <summary>离开页面：取消后台日志任务并记录日志。</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Logger.LogInformation("离开日志显示页面");
            CancellationTokenSource.Cancel();
        }

        /// <summary>进入页面：重建取消源，并启动后台循环按固定间隔输出各级别测试日志。</summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 重新创建 CTS，比 TryReset 更可靠
            CancellationTokenSource = new CancellationTokenSource();
            Logger.LogInformation("进入日志显示页面");

            Task.Run(() => {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                     Logger.LogInformation("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                     Logger.LogDebug("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                     Logger.LogError("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                     Logger.LogCritical("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                     Logger.LogTrace("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                     Logger.LogWarning("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                     Task.Delay(1).Wait();
                }
            }, CancellationTokenSource.Token);
        }

        #endregion
    }
}
