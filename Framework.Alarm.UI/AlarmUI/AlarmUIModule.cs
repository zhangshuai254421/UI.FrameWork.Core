using Prism.Ioc;
using Prism.Modularity;
using UI.FrameWork.Core.Main;

namespace Framework.Alarm.UI
{
    /// <summary>
    /// 报警 UI 模块：装进模块目录 = 宿主采用报警呈现——报警列表页注册为可导航视图，
    /// 顶栏桥接通（活跃快照 → 页头三件套），报警按钮亮出。不装本模块，壳照常运行、顶栏按钮不出现：
    /// 「给项目决定是否使用」由这一行 AddModule 决定（仿 RecipeUIModule 的可选装配模式）。
    /// </summary>
    public class AlarmUIModule : IModule
    {
        /// <summary>模块初始化：接通顶栏桥（补初始快照），并亮出顶栏报警按钮。</summary>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<AlarmHeaderBridge>().Connect();
            containerProvider.Resolve<HeaderViewModel>().IsShowAlarm = true;
        }

        /// <summary>注册本模块的类型：报警列表页（视图配 VM）与顶栏桥（单例）。</summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<AlarmListView, AlarmListViewModel>();
            containerRegistry.RegisterSingleton<AlarmHeaderBridge>();
        }
    }
}
