using Framework.Alarm;
using PrismUI.Core;
using SemiAppliaction.Views;

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
            containerProvider.Resolve<INavigationService>().NavigateToAsync(nameof(MainView));
        }

        /// <summary>模块类型注册：把主视图与各测试页连同其 ViewModel 注册为可导航视图；
        /// 另把演示采集层注册为条件源——引擎由壳的 AddAlarmEngine 构造，条件源是宿主的事，
        /// 本宿主还没有真采集层，注册剧本源让演示报警点得亮顶栏（真宿主换成自己的采集层）。</summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>();
            containerRegistry.RegisterForNavigation<TestView, TestViewModel>();
            containerRegistry.RegisterForNavigation<CameraTestView, CameraTestViewModel>();
            containerRegistry.RegisterForNavigation<DetectionTestView, DetectionTestViewModel>();
            containerRegistry.RegisterForNavigation<AlarmTestView, AlarmTestViewModel>();
            // 条件源先注册具体单例再映射接口：引擎（IConditionSource）与演示装置（剧本推数）必须拿到同一个实例
            containerRegistry.RegisterSingleton<SimulatedConditionSource>();
            containerRegistry.RegisterSingleton(typeof(IConditionSource), sp => sp.Resolve<SimulatedConditionSource>());
        }
    }
}
