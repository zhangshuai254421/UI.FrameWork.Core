using Framework.Core.Common;
using Microsoft.Extensions.Logging;
using PrismUI.Core;
using Recipe.UI.RecipeUI;
using SemiAppliaction.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.FrameWork.Core.Main.Footer;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：Main 视图模型，负责与主界面交互的逻辑（示例：记录日志）
    public class MainViewModel : BindableBase
    {
        private readonly ILogger<MainViewModel> _logger;
        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;
            //_logger.LogDebug("测试");
        }

        //ShowRecipe：导航到配方主页
        private DelegateCommand _ShowRecipe = null!;
        public DelegateCommand ShowRecipe => _ShowRecipe ?? new DelegateCommand(() =>
        {
            IoC.Get<INavigationService>().NavigateToAsync(nameof(RecipeHomeView), RegionNames.MainRegion);
        });

        private DelegateCommand _ShowTest = null!;
        public DelegateCommand ShowTestCommand => _ShowTest ?? new DelegateCommand(() =>
        {
            IoC.Get<INavigationService>().NavigateToAsync(nameof(TestView), RegionNames.MainRegion);
        });
    }
}