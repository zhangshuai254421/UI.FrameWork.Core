using System;

namespace Recipe.UI.RecipeUI
{
    /// <summary>
    /// 创建文件夹弹框：输入新文件夹名，确认后通过 NewGroupName 参数返回。
    /// </summary>
    public class NewGroupDialogViewModel : BindableBase, IDialogAware
    {
        #region 构造函数

        /// <summary>Prism 注入的对话框关闭监听器，用于携带结果关闭对话框。</summary>
        public DialogCloseListener RequestClose { get; }

        /// <summary>构造函数：注入对话框关闭监听器。</summary>
        public NewGroupDialogViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
        }

        #endregion

        #region 属性

        /// <summary>新文件夹名（用户输入）的后备字段。</summary>
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

        /// <summary>校验错误提示文本的后备字段。</summary>
        private string _errorMessage = string.Empty;
        /// <summary>
        /// 校验错误提示
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>是否存在校验错误的后备字段。</summary>
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

        /// <summary>保存命令：以 OK 结果携带新文件夹名关闭对话框；有校验错误或输入为空时禁用。</summary>
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

        /// <summary>取消命令：以 Cancel 结果关闭对话框，不创建文件夹。</summary>
        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        });

        #endregion

        #region IDialogAware

        /// <summary>是否允许关闭对话框；本对话框始终允许。</summary>
        public bool CanCloseDialog()
        {
            return true;
        }

        /// <summary>对话框关闭后的回调；本对话框无需要清理的资源。</summary>
        public void OnDialogClosed()
        {
            // 弹框关闭时的清理逻辑（如需要）
        }

        /// <summary>对话框打开时的初始化：清空新文件夹名输入。</summary>
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
