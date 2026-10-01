
using System.Reflection;

namespace PrismUI.Core
{
    /// <summary>
    /// Prism UI 核心模块：注册通用弹框等 UI 基础服务。
    /// </summary>
    public class PrismUIModule(IRegionManager regionManager) : IModule
    {
        /// <summary>模块初始化回调；当前无需额外初始化。</summary>
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        /// <summary>注册本模块的类型与服务，当前注册通用确认弹框。</summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册弹框：View 类型 → ViewModel 类型 或 自动匹配
            containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        }
    }

}
