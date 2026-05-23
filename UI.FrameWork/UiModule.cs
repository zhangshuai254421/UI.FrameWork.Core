using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SemiAppliaction.Core;

namespace SemiAppliaction
{

    // 作者：Zhang Shuai
    // 描述：Prism 模块，用于向模块目录注册视图和区域（例如将 MainView 注册到 MainRegion）。
    public class UiModule(IRegionManager regionManager) : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion("MainRegion", typeof(MainView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //throw new NotImplementedException();
        }
    }
}
