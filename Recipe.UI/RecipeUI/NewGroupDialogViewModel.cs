using System;

namespace Recipe.UI.RecipeUI
{
    /// <summary>
    /// 创建文件夹弹框：输入新文件夹名，确认后通过 NewGroupName 参数返回。
    /// </summary>
    public class NewGroupDialogViewModel : BindableBase, IDialogAware
    {
        #region 构造函数

        public DialogCloseListener RequestClose { get; }

        public NewGroupDialogViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
        }

        #endregion

        #region 属性

        private string _newGroupName = string.Empty;
        /// <summary>
        /// 新文件夹名（用户输入）
        /// </summary>
        public string NewGroupName
        {
            get => _newGroupName;
            set
            {
                if (SetProperty(ref _newGroupName, value))
                {
                    // 实时校验
                    Validate();
                }
            }
        }

        private string _errorMessage = string.Empty;
        /// <summary>
        /// 校验错误提示
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _hasError;
        /// <summary>
        /// 是否有校验错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand => new DelegateCommand(() =>
        {
            // 返回新的文件夹名
            var parameters = new DialogParameters
            {
                { "NewGroupName", NewGroupName?.Trim() },
                { "IsSaved", true }
            };
            var result = new DialogResult(ButtonResult.OK);
            result.Parameters = parameters;
            RequestClose.Invoke(result);
        },
        // 只有校验通过时才能点击确认
        () => !HasError && !string.IsNullOrWhiteSpace(NewGroupName))
        .ObservesProperty(() => HasError)
        .ObservesProperty(() => NewGroupName);

        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        });

        #endregion

        #region IDialogAware

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // 弹框关闭时的清理逻辑（如需要）
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            NewGroupName = string.Empty; // 新建：不预填名称
        }

        #endregion

        #region 校验

        /// <summary>
        /// 实时校验（输入时触发）
        /// </summary>
        private void Validate()
        {
            var trimmed = NewGroupName?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(trimmed) && string.IsNullOrWhiteSpace(trimmed))
            {
                ErrorMessage = "文件夹名不能为空白字符";
                HasError = true;
                return;
            }

            ErrorMessage = string.Empty;
            HasError = false;
        }

        #endregion
    }
}
