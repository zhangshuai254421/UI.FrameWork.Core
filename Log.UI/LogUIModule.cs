using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.UI
{


    /// <summary>
    /// 日志 UI 模块：向 Prism 注册日志查看页的导航视图。
    /// </summary>
    public class LogUIModule(IRegionManager regionManager) : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<LogViewerView>();

            // 注册弹框：View 类型 → ViewModel 类型 或 自动匹配
            //containerRegistry.RegisterDialog<LogViewerView, LogViewerViewModel>();


        }
    }
}
