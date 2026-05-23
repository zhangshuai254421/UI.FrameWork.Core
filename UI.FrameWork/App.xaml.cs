using Microsoft.Extensions.DependencyInjection;
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
            // 注册公共模块
            moduleCatalog.AddModule<UiModule>();
            moduleCatalog.AddModule<FrameworkModule>();
            base.ConfigureModuleCatalog(moduleCatalog);

        }
    }

}
