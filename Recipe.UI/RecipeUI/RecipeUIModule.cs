using Framework.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recipe.UI.RecipeUI
{
    /// <summary>
    /// 配方 UI 模块：注册配方主页的导航视图与重命名、复制、新建文件夹三个弹框。
    /// </summary>
    public class RecipeUIModule(IRegionManager regionManager) : IModule
    {
        /// <summary>模块初始化回调；本模块无需额外初始化逻辑。</summary>
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        /// <summary>注册本模块的类型：配方主页导航视图与各弹框。</summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<RecipeHomeView>();

            // 注册弹框：View 类型 → ViewModel 类型 或 自动匹配
            containerRegistry.RegisterDialog<RenameRecipeDialogView, RenameRecipeDialogViewModel>();
            containerRegistry.RegisterDialog<CopyRecipeDialogView, CopyRecipeDialogViewModel>();
            containerRegistry.RegisterDialog<NewGroupDialogView, NewGroupDialogViewModel>();


        }
    }
}
