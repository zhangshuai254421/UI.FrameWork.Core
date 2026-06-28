using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recipe.UI.RecipeUI
{
    public class RenameRecipeDialogViewModel : BindableBase, IDialogAware
    {
        public DialogCloseListener RequestClose { get; }
        public RenameRecipeDialogViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            SaveCommand.Execute();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {

        }

        // ==================== 命令 ====================

        public DelegateCommand SaveCommand => new DelegateCommand(() =>
        {
            // 准备好返回数据
        //    var parameters = new DialogParameters
        //{
        //    { "UserName", UserName },
        //    { "Email", Email },
        //    { "Phone", Phone },
        //    { "IsSaved", true }
        //};

            // 关闭弹框并返回 OK
            RequestClose.Invoke(new DialogResult(ButtonResult.OK/*, parameters*/));
        });

        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            // 关闭弹框并返回 Cancel
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        });

      
    }
}
