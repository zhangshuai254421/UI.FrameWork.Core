using Microsoft.Extensions.DependencyInjection;
using PrismUI.Core;
using Recipe.UI.RecipeUI;
using System.Configuration;
using System.Data;
using System.Windows;
using UI.FrameWork.Core;

namespace SemiAppliaction.Core
{
    // 作者：Zhang Shuai
    // 描述：应用程序入口，继承自 FrameworkAppBase 用于初始化框架、模块目录和依赖注入配置。
    public partial class App : FrameworkAppBase
    {
        public App()
        {

        }
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            // 注册公共模块（FrameworkModule 先加载，确保 BaseView 的 Region 先就绪）
            moduleCatalog.AddModule<FrameworkModule>();
            moduleCatalog.AddModule<UIModule>();
            moduleCatalog.AddModule<RecipeUIModule>();
            moduleCatalog.AddModule<PrismUIModule>();
            base.ConfigureModuleCatalog(moduleCatalog);

        }
    }

}
