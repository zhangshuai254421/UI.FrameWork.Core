
using System.Reflection;

namespace PrismUI.Core
{
    public class PrismUIModule(IRegionManager regionManager) : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册弹框：View 类型 → ViewModel 类型 或 自动匹配
            containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        }
    }

}
