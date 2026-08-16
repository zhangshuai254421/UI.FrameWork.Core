using Framework.Core.Common;
using Microsoft.Extensions.Logging;
using PrismUI.Core;
using Recipe.Domain;
using Recipe.Infrastructure;
using Recipe.UI.RecipeUI;
using SemiAppliaction.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.FrameWork.Core.Main.Footer;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：Main 视图模型，负责与主界面交互的逻辑（示例：记录日志）。f
    public class MainViewModel:BindableBase
    {
        private readonly ILogger<MainViewModel> _logger;
        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;
            //_logger.LogDebug("测试");
        }
        //ShowRecipe
        private DelegateCommand _ShowRecipe = null!;
        public DelegateCommand ShowRecipe => _ShowRecipe ?? new DelegateCommand(() =>
        {

            var svc = IoC.Get<GenericRecipeParameterService<PressureParameter, Guid>>();
            svc.AddAsync(new PressureParameter { RecipeId = 1, TargetPressure = 0.5 });
            var list = svc.GetCurrentRecipeParameterAsync();

            var temp=IoC.Get<GenericSystemParemeterService<ComputerParameter, Guid>>();
            temp.AddAsync(new ComputerParameter { Name = "Computer1" });
            var list2 = temp.GetListAsync().Result;


            IoC.Get<INavigationService>().NavigateToAsync(nameof(RecipeHomeView), RegionNames.MainRegion);
        });
    }
}
