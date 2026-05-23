using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace UI.FrameWork.Core.Main
{
    public class LoginViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogger<LoginViewModel> _logger;
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

        private string _username = "";
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public DelegateCommand LoginCommand { get; private set; }

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
