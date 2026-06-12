using Framework.Core.Common;
using Recipe.UI.RecipeUI;
using UI.FrameWork.Core.Main;
using UI.FrameWork.Core.Main.Footer;
namespace UI.FrameWork.Core;

public class FrameworkModule(IRegionManager regionManager  )  : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        regionManager.RegisterViewWithRegion(RegionNames.HeaderViewRegion, typeof(HeaderView));
        regionManager.RegisterViewWithRegion(RegionNames.FooterRegion, typeof(FooterView));
        //regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(RecipeHomeView));
        
        //// 注册导航
        //_containerRegistry.RegisterForNavigation<HeaderView>();
        //_containerRegistry.RegisterForNavigation<NumberKeyPadView>();
        
        //// 跳转
        //_regionManager.RequestNavigate("ToolBoxRegion", "HeaderView");
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        //containerRegistry.RegisterForNavigation<ShellWindow,ShellWindowModel>();
        // 注册 ViewModel（需要手动控制布局的）


        // 注册导航
       containerRegistry.RegisterViewsFromAssembly(GetType().Assembly);

       
    }
}