using Log.UI;
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
        /// <summary>空构造：初始化工作全部由基类 FrameworkAppBase 完成。</summary>
        public App()
        {

        }
        /// <summary>重写模块目录配置：按加载顺序注册框架与各业务模块，FrameworkModule 需最先加载。</summary>
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            // 注册公共模块（FrameworkModule 先加载，确保 BaseView 的 Region 先就绪）
            moduleCatalog.AddModule<FrameworkModule>();
            moduleCatalog.AddModule<UIModule>();
            moduleCatalog.AddModule<RecipeUIModule>();
            moduleCatalog.AddModule<PrismUIModule>();
            moduleCatalog.AddModule<LogUIModule>();
            base.ConfigureModuleCatalog(moduleCatalog);

        }
    }

}
