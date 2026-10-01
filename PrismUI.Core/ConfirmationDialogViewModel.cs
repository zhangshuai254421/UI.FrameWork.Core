using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrismUI.Core
{
    /// <summary>
    /// 确认对话框的 ViewModel：承载标题、正文与按钮文本，确定时以 Yes 关闭，取消时以 No 关闭。
    /// </summary>
    public class ConfirmationDialogViewModel : BindableBase, IDialogAware
    {
        /// <summary>对话框关闭监听器，由 Prism 注入，用于携带结果请求关闭对话框。</summary>
        public DialogCloseListener RequestClose { get; }
        /// <summary>构造函数：注入对话框关闭监听器。</summary>
        public  ConfirmationDialogViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
        }
        // ==================== 绑定属性 ====================

        /// <summary>标题的 backing 字段，默认"确认"。</summary>
        private string _title = "确认";
        /// <summary>对话框标题。</summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>正文消息的 backing 字段。</summary>
        private string _message;
        /// <summary>对话框正文消息。</summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>确定按钮文本的 backing 字段，默认"确定"。</summary>
        private string _okButtonText = "确定";
        /// <summary>确定按钮文本。</summary>
        public string OKButtonText
        {
            get => _okButtonText;
            set => SetProperty(ref _okButtonText, value);
        }

        /// <summary>取消按钮文本的 backing 字段，默认"取消"。</summary>
        private string _cancelButtonText ="取消";
        /// <summary>取消按钮文本。</summary>
        public string CancelButtonText
        {
            get => _cancelButtonText;
            set => SetProperty(ref _cancelButtonText, value);
        }

        // 控制取消按钮是否可见（仅确认时隐藏）
        private bool _showCancelButton = true;
        /// <summary>是否显示取消按钮；仅确认场景可隐藏取消按钮。</summary>
        public bool ShowCancelButton
        {
            get => _showCancelButton;
            set => SetProperty(ref _showCancelButton, value);
        }

        // ==================== IDialogAware ====================



        /// <summary>是否允许关闭对话框；始终返回 true，不做拦截。</summary>
        public bool CanCloseDialog() => true;

        /// <summary>对话框打开时由 Prism 调用：从参数读取标题、正文、按钮文本与取消按钮可见性。</summary>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 从调用方接收参数
            if (parameters.ContainsKey("Title"))
                Title = parameters.GetValue<string>("Title");

            if (parameters.ContainsKey("Message"))
                Message = parameters.GetValue<string>("Message");

            if (parameters.ContainsKey("OKButtonText"))
                OKButtonText = parameters.GetValue<string>("OKButtonText");

            if (parameters.ContainsKey("CancelButtonText"))
                CancelButtonText = parameters.GetValue<string>("CancelButtonText");

            if (parameters.ContainsKey("ShowCancelButton"))
                ShowCancelButton = parameters.GetValue<bool>("ShowCancelButton");
        }

        /// <summary>对话框关闭后由 Prism 调用；当前无需额外清理。</summary>
        public void OnDialogClosed() { }

        // ==================== 命令 ====================

        /// <summary>确定命令：以 Yes 结果关闭对话框。</summary>
        public DelegateCommand OKCommand => new DelegateCommand(() =>
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Yes));
        });

        /// <summary>取消命令：以 No 结果关闭对话框。</summary>
        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.No));
        });

       
    }
}
