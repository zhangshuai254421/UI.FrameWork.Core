using System;

namespace Recipe.UI.RecipeUI
{
    /// <summary>
    /// 复制配方对话框的视图模型：输入新配方名与目标文件夹，实时校验后携带结果关闭对话框。
    /// </summary>
    public class CopyRecipeDialogViewModel : BindableBase, IDialogAware
    {
        #region 字段

        /// <summary>原始配方名。</summary>
        private string _originalRecipeName = string.Empty;
        /// <summary>新配方名（用户输入）。</summary>
        private string _newRecipeName = string.Empty;
        /// <summary>目标文件夹名（用户输入）。</summary>
        private string _targetGroupName = string.Empty;
        /// <summary>可选文件夹列表（下拉提示）。</summary>
        private IEnumerable<string> _folderNames = Array.Empty<string>();
        /// <summary>校验错误提示文本。</summary>
        private string _errorMessage = string.Empty;
        /// <summary>是否存在校验错误。</summary>
        private bool _hasError;

        #endregion

        #region 构造函数

        /// <summary>Prism 注入的对话框关闭监听器，用于携带结果关闭对话框。</summary>
        public DialogCloseListener RequestClose { get; }

        /// <summary>构造函数：注入对话框关闭监听器。</summary>
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

        /// <summary>保存命令：最终校验通过后携带新配方名与目标文件夹，以 OK 结果关闭对话框；有校验错误或输入为空时禁用。</summary>
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

        /// <summary>取消命令：以 Cancel 结果关闭对话框，不保存任何更改。</summary>
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
        }

        /// <summary>对话框打开时读取传入参数：原配方名、当前文件夹与可选文件夹列表，并以此初始化各输入项。</summary>
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