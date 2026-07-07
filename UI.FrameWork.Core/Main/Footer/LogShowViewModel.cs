using Framework.Core.Common;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.RichTextBox.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main.Footer
{
    public class LogShowViewModel : BindableBase, INavigationAware
    {
        private readonly ILogger<LogShowViewModel> _logger;
        public LogShowViewModel(ILogger<LogShowViewModel> logger) {
            _logger = logger;
        }
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;  // 复用同一个 View 实例，避免重复创建导致 Serilog sink 丢失
        }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _logger.LogInformation("离开日志显示页面");
            CancellationTokenSource.Cancel();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 重新创建 CTS，比 TryReset 更可靠
            CancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("进入日志显示页面");
           
            Task.Run(() => {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    //_logger.LogInformation("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                    //_logger.LogDebug("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                    //_logger.LogError("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                    //_logger.LogCritical("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                    //_logger.LogTrace("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                    //_logger.LogWarning("这是一个测试日志，当前时间：{time}", DateTime.Now.ToString("fffffff"));
                    //Task.Delay(1).Wait();
                }
            }, CancellationTokenSource.Token);
        }


    }
}
