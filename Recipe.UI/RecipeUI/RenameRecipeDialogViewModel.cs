using System;

namespace Recipe.UI.RecipeUI
{
    /// <summary>
    /// 重命名配方对话框的视图模型：输入新配方名，实时校验后携带结果关闭对话框。
    /// </summary>
    public class RenameRecipeDialogViewModel : BindableBase, IDialogAware
    {
        /// <summary>Prism 注入的对话框关闭监听器，用于携带结果关闭对话框。</summary>
        public DialogCloseListener RequestClose { get; }

        /// <summary>构造函数：注入对话框关闭监听器。</summary>
        public RenameRecipeDialogViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
        }

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

        /// <summary>对话框打开时读取原配方名，并把新配方名预填为原名方便用户微调。</summary>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 从参数中获取原始配方名
            if (parameters.ContainsKey("RecipeName"))
            {
                OriginalRecipeName = parameters.GetValue<string>("RecipeName");
                // 默认将新配方名也设为原名（方便用户微调）
                NewRecipeName = OriginalRecipeName;
            }
        }

        // ==================== 属性 ====================

        /// <summary>原配方名的后备字段。</summary>
        private string _originalRecipeName = string.Empty;
        /// <summary>
        /// 原配方名（只读显示）
        /// </summary>
        public string OriginalRecipeName
        {
            get => _originalRecipeName;
            set => SetProperty(ref _originalRecipeName, value);
        }

        /// <summary>新配方名（用户输入）的后备字段。</summary>
        private string _newRecipeName = string.Empty;
        /// <summary>
        /// 新配方名（用户输入）
        /// </summary>
        public string NewRecipeName
        {
            get => _newRecipeName;
            set
            {
                if (SetProperty(ref _newRecipeName, value))
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

        // ==================== 命令 ====================

        /// <summary>保存命令：最终校验通过后携带新配方名，以 OK 结果关闭对话框；有校验错误或输入为空时禁用。</summary>
        public DelegateCommand SaveCommand => new DelegateCommand(() =>
        {
            // 最终校验
            if (!ValidateForSave())
            {
                return;
            }

            // 返回新的配方名
            var parameters = new DialogParameters
            {
                { "NewRecipeName", NewRecipeName?.Trim() },
                { "IsSaved", true }
            };
            var result = new DialogResult(ButtonResult.OK);
            result.Parameters = parameters;
            RequestClose.Invoke(result);
        },
        // 只有校验通过时才能点击确认
        () => !HasError && !string.IsNullOrWhiteSpace(NewRecipeName))
        .ObservesProperty(() => HasError)
        .ObservesProperty(() => NewRecipeName);

        /// <summary>取消命令：以 Cancel 结果关闭对话框，不保存更改。</summary>
        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        });

        // ==================== 校验 ====================

        /// <summary>
        /// 实时校验（输入时触发）
        /// </summary>
        private void Validate()
        {
            var trimmed = NewRecipeName?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(trimmed))
            {
                ErrorMessage = string.Empty;
                HasError = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                ErrorMessage = "配方名不能为空白字符";
                HasError = true;
                return;
            }

            // 检查是否与原名称相同
            if (trimmed == (OriginalRecipeName?.Trim() ?? string.Empty))
            {
                ErrorMessage = "新配方名与原配方名相同";
                HasError = true;
                return;
            }

            ErrorMessage = string.Empty;
            HasError = false;
        }

        /// <summary>
        /// 保存前最终校验
        /// </summary>
        private bool ValidateForSave()
        {
            var trimmed = NewRecipeName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                ErrorMessage = "请输入新的配方名";
                HasError = true;
                return false;
            }

            if (trimmed == (OriginalRecipeName?.Trim() ?? string.Empty))
            {
                ErrorMessage = "新配方名与原配方名相同";
                HasError = true;
                return false;
            }

            return true;
        }
    }
}
