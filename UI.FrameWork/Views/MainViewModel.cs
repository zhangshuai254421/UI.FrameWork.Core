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
        /// <summary>日志记录器。</summary>
        private readonly ILogger<MainViewModel> _logger;
        /// <summary>构造：注入日志记录器。</summary>
        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;
            //_logger.LogDebug("测试");
        }

        //ShowRecipe：导航到配方主页
        private DelegateCommand _ShowRecipe = null!;
        /// <summary>打开配方主页命令：绑定主页面的配方入口按钮，导航到配方主页。</summary>
        public DelegateCommand ShowRecipe => _ShowRecipe ?? new DelegateCommand(() =>
        {
            IoC.Get<INavigationService>().NavigateToAsync(nameof(RecipeHomeView), RegionNames.MainRegion);
        });

        /// <summary>打开测试页命令的缓存实例。</summary>
        private DelegateCommand _ShowTest = null!;
        /// <summary>打开测试页命令：绑定主页面的测试入口按钮，导航到测试页。</summary>
        public DelegateCommand ShowTestCommand => _ShowTest ?? new DelegateCommand(() =>
        {
            IoC.Get<INavigationService>().NavigateToAsync(nameof(TestView), RegionNames.MainRegion);
        });
    }
}