using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using UI.FrameWork.Core;

namespace SemiAppliaction.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
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
