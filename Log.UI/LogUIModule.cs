using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.UI
{


    public class LogUIModule(IRegionManager regionManager) : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<LogViewerView>();

            // 注册弹框：View 类型 → ViewModel 类型 或 自动匹配
            //containerRegistry.RegisterDialog<LogViewerView, LogViewerViewModel>();


        }
    }
}
