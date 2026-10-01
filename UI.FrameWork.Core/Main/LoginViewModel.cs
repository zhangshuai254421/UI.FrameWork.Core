using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace UI.FrameWork.Core.Main
{
    /// <summary>登录窗 ViewModel：维护用户名与密码输入，执行登录并关闭登录窗口。</summary>
    public class LoginViewModel : BindableBase
    {
        /// <summary>区域管理器（预留，当前登录逻辑未使用）。</summary>
        private readonly IRegionManager _regionManager;
        /// <summary>日志记录器，用于记录登录尝试。</summary>
        private readonly ILogger<LoginViewModel> _logger;
        /// <summary>构造：注入区域管理器与日志，并初始化登录命令。</summary>
        public LoginViewModel(IRegionManager regionManager, ILogger<LoginViewModel> logger)
        {
            _logger = logger;
            _regionManager = regionManager;
            LoginCommand = new DelegateCommand(OnLogin);
            //Task.Run(async () =>
            //{
            //    await Task.Delay(300);
            //    OnLogin();
            //});
        }

        /// <summary>用户名输入的后端字段。</summary>
        private string _username = "";
        /// <summary>用户名输入，绑定登录窗用户名文本框。</summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>密码输入的后端字段，由视图的 PasswordBox 变更事件同步。</summary>
        private string _password = "";
        /// <summary>密码输入；PasswordBox 不做双向绑定，由视图事件写入。</summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>登录错误提示的后端字段。</summary>
        private string? _errorMessage;
        /// <summary>登录错误提示文本，绑定登录窗提示区域；为空表示无错误。</summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>登录命令，绑定登录窗登录按钮。</summary>
        public DelegateCommand LoginCommand { get; private set; }

        /// <summary>登录处理：模拟校验延时后记录日志并关闭登录窗口；正式鉴权逻辑暂被注释保留。</summary>
        async void OnLogin()
        {
            await Task.Delay(300);

            _logger.LogInformation("User {Username} is attempting to log in.", Username);
            var str = string.Empty;
            Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)
                ?.Close();
        }
        //if (_authService.Login(Username, Password))
        //{
        //    // 登录成功：关闭当前窗口，打开主窗口
        //    //var mainWindow = new Views.Shell();
        //    //mainWindow.Show();

        //    // 关闭当前登录窗口
        //    Application.Current.Windows
        //        .OfType<Window>()
        //        .FirstOrDefault(w => w.DataContext == this)
        //        ?.Close();
        //}
        //else
        //{
        //    ErrorMessage = "用户名或密码错误！";
        //}
    
    }
}
