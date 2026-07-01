using System;

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
            // 弹框关闭时的清理逻辑（如需要）
        }

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

        private string _originalRecipeName = string.Empty;
        /// <summary>
        /// 原配方名（只读显示）
        /// </summary>
        public string OriginalRecipeName
        {
            get => _originalRecipeName;
            set => SetProperty(ref _originalRecipeName, value);
        }

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

        // ==================== 命令 ====================

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
