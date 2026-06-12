using Framework.Core.Common;
using SemiAppliaction.Core;
using SemiAppliaction.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.FrameWork.Core.Main;

namespace SemiAppliaction
{

    // 作者：Zhang Shuai
    // 描述：Prism 模块，用于向模块目录注册视图和区域（例如将 MainView 注册到 MainRegion）。
    public class UIModule(IRegionManager regionManager) : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion("MainRegion", typeof(MainView));
            //IoC.Get<INavigationService>().NavigateToAsync(nameof(MainView));
            containerProvider.Resolve<INavigationService>().NavigateToAsync(nameof(MainView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //throw new NotImplementedException();
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>();
        }
    }
}
