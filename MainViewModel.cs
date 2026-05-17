using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemiAppliaction
{
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
