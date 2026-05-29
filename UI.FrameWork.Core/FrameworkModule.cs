using UI.FrameWork.Core.Main;
using UI.FrameWork.Core.Main.View;

namespace UI.FrameWork.Core;

public class FrameworkModule(IRegionManager regionManager  )  : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        regionManager.RegisterViewWithRegion("HeaderViewRegion", typeof(HeaderView));
        regionManager.RegisterViewWithRegion("FooterRegion", typeof(FooterView));
        regionManager.RegisterViewWithRegion("MainRegion", typeof(BaseView));
        regionManager.RegisterViewWithRegion("BaseViewMainRegion", typeof(LogViewerView));
        //// 注册导航
        //_containerRegistry.RegisterForNavigation<HeaderView>();
        //_containerRegistry.RegisterForNavigation<NumberKeyPadView>();

        //// 跳转
        //_regionManager.RequestNavigate("ToolBoxRegion", "HeaderView");
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册导航
        containerRegistry.RegisterForNavigation<HeaderView>();
        containerRegistry.RegisterForNavigation<NumberKeyPadView>();
        containerRegistry.RegisterForNavigation<DirectView>();
        containerRegistry.RegisterForNavigation<LogViewerView>();
        containerRegistry.RegisterForNavigation<BaseView>();
    }
}