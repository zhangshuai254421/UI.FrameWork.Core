using System;

namespace Recipe.UI.RecipeUI
{
    public class CopyRecipeDialogViewModel : BindableBase, IDialogAware
    {
        #region 字段

        private string _originalRecipeName = string.Empty;
        private string _newRecipeName = string.Empty;
        private string _targetGroupName = string.Empty;
        private IEnumerable<string> _folderNames = Array.Empty<string>();
        private string _errorMessage = string.Empty;
        private bool _hasError;

        #endregion

        #region 构造函数

        public DialogCloseListener RequestClose { get; }

        public CopyRecipeDialogViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 原配方名（只读显示）
        /// </summary>
        public string OriginalRecipeName
        {
            get => _originalRecipeName;
            set => SetProperty(ref _originalRecipeName, value);
        }

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
                    Validate();
                }
            }
        }

        /// <summary>
        /// 目标文件夹名（用户输入；可选已有文件夹，也可输入新文件夹名）
        /// </summary>
        public string TargetGroupName
        {
            get => _targetGroupName;
            set
            {
                if (SetProperty(ref _targetGroupName, value))
                {
                    Validate();
                }
            }
        }

        /// <summary>
        /// 可选文件夹列表（下拉提示）
        /// </summary>
        public IEnumerable<string> FolderNames
        {
            get => _folderNames;
            set => SetProperty(ref _folderNames, value);
        }

        /// <summary>
        /// 校验错误提示
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

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
            if (!ValidateForSave())
            {
                return;
            }

            var parameters = new DialogParameters
            {
                { "NewRecipeName", NewRecipeName?.Trim() },
                { "NewGroupName", TargetGroupName?.Trim() },
                { "IsSaved", true }
            };
            var result = new DialogResult(ButtonResult.OK);
            result.Parameters = parameters;
            RequestClose.Invoke(result);
        },
        () => !HasError && !string.IsNullOrWhiteSpace(NewRecipeName) && !string.IsNullOrWhiteSpace(TargetGroupName))
        .ObservesProperty(() => HasError)
        .ObservesProperty(() => NewRecipeName)
        .ObservesProperty(() => TargetGroupName);

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
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("RecipeName"))
            {
                OriginalRecipeName = parameters.GetValue<string>("RecipeName");
                NewRecipeName = OriginalRecipeName;
            }

            // 目标文件夹：默认当前文件夹，可下拉选已有文件夹，也可输入新文件夹名
            if (parameters.ContainsKey("GroupName"))
            {
                TargetGroupName = parameters.GetValue<string>("GroupName") ?? string.Empty;
            }

            if (parameters.ContainsKey("GroupNames"))
            {
                FolderNames = parameters.GetValue<IEnumerable<string>>("GroupNames") ?? Array.Empty<string>();
            }
        }

        #endregion

        #region 校验

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

            // 目标文件夹名
            var target = TargetGroupName?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(target) && string.IsNullOrWhiteSpace(target))
            {
                ErrorMessage = "文件夹名不能为空白字符";
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

            if (string.IsNullOrWhiteSpace(TargetGroupName))
            {
                ErrorMessage = "请选择或输入目标文件夹";
                HasError = true;
                return false;
            }

            return true;
        }

        #endregion
    }
}