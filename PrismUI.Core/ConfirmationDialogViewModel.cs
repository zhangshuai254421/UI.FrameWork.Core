using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrismUI.Core
{
    public class ConfirmationDialogViewModel : BindableBase, IDialogAware
    {
        public DialogCloseListener RequestClose { get; }
        public  ConfirmationDialogViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
        }
        // ==================== 绑定属性 ====================

        private string _title = "确认";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private string _okButtonText = "确定";
        public string OKButtonText
        {
            get => _okButtonText;
            set => SetProperty(ref _okButtonText, value);
        }

        private string _cancelButtonText ="取消";
        public string CancelButtonText
        {
            get => _cancelButtonText;
            set => SetProperty(ref _cancelButtonText, value);
        }

        // 控制取消按钮是否可见（仅确认时隐藏）
        private bool _showCancelButton = true;
        public bool ShowCancelButton
        {
            get => _showCancelButton;
            set => SetProperty(ref _showCancelButton, value);
        }

        // ==================== IDialogAware ====================



        public bool CanCloseDialog() => true;

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

        public void OnDialogClosed() { }

        // ==================== 命令 ====================

        public DelegateCommand OKCommand => new DelegateCommand(() =>
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Yes));
        });

        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.No));
        });

       
    }
}
