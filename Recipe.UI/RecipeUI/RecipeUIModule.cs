using Framework.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recipe.UI.RecipeUI
{
    public class RecipeUIModule(IRegionManager regionManager) : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<RecipeHomeView>();

            // 注册弹框：View 类型 → ViewModel 类型 或 自动匹配
            containerRegistry.RegisterDialog<RenameRecipeDialogView, RenameRecipeDialogViewModel>();
            containerRegistry.RegisterDialog<CopyRecipeDialogView, CopyRecipeDialogViewModel>();


        }
    }
}
