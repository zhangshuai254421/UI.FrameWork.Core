using Framework.Core.Common;
using PrismUI.Core;
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
        /// <summary>模块初始化：把主视图注册到主区域，并立即导航到主视图。</summary>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion("MainRegion", typeof(MainView));
            //IoC.Get<INavigationService>().NavigateToAsync(nameof(MainView));
            containerProvider.Resolve<INavigationService>().NavigateToAsync(nameof(MainView));

        }

        /// <summary>模块类型注册：把主视图与各测试页连同其 ViewModel 注册为可导航视图。</summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //throw new NotImplementedException();
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>();
            containerRegistry.RegisterForNavigation<TestView, TestViewModel>();
            containerRegistry.RegisterForNavigation<CameraTestView, CameraTestViewModel>();
            containerRegistry.RegisterForNavigation<DetectionTestView, DetectionTestViewModel>();
        }
    }
}
