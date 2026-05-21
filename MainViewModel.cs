using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemiAppliaction
{
    // 作者：Zhang Shuai
    // 描述：Main 视图模型，负责与主界面交互的逻辑（示例：记录日志）。
    public class MainViewModel
    {
        private readonly ILogger<MainViewModel> _logger;
        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;
            _logger.LogDebug("测试");
        }
    }
}
