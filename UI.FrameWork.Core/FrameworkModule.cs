using Framework.Core.Common;
using PrismUI.Core;
using Recipe.UI.RecipeUI;
using UI.FrameWork.Core.Main;
using UI.FrameWork.Core.Main.Footer;
namespace UI.FrameWork.Core;

/// <summary>
/// 框架 UI 模块：把页头、页脚视图注册到对应区域，并从本程序集批量注册可导航视图。
/// </summary>
public class FrameworkModule(IRegionManager regionManager  )  : IModule
{
    /// <summary>模块初始化：把页头视图与页脚视图绑定到各自的命名区域，实现按区域自动加载。</summary>
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

    /// <summary>模块类型注册：从本程序集批量注册视图与 ViewModel，供区域导航按名解析。</summary>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        //containerRegistry.RegisterForNavigation<ShellWindow,ShellWindowModel>();
        // 注册 ViewModel（需要手动控制布局的）


        // 注册导航
       containerRegistry.RegisterViewsFromAssembly(GetType().Assembly);

        // 页头 VM 注册成单例且放在批量注册之后：视图定位器按名字约定解析会自己 new 一个，
        // 这条覆盖它，页头桥与视图绑定才拿到同一个实例
        containerRegistry.RegisterSingleton<HeaderViewModel>();

    }
}