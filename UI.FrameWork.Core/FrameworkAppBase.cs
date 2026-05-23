using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Serilog;
using UI.FrameWork.Core.Common;
using UI.FrameWork.Core.Main;

namespace UI.FrameWork.Core
{

    public abstract class FrameworkAppBase : PrismApplicationBase
    {
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        protected override IContainerExtension CreateContainerExtension()
        {
            //IServiceCollection services = new ServiceCollection();
            return new DryIocContainerExtension();
        }
        protected override Window CreateShell()
        {
            return  Container.Resolve<ShellWindow>(); ;
        }
        private static Mutex AppMutex;
        /// <summary>
        /// TODO 3
        /// </summary>
        protected override void OnInitialized()
        {
            IoC.GetInstance = this.Container.Resolve;
            //IoC.BuildUp = this.Container.BuildUp;

             //var  s=IoC.Get<FooterViewModel>();
            bool createdNew = false;
            FrameworkAppBase.AppMutex = new Mutex(true, "title", out createdNew);
            bool flag6 = !createdNew;
            if (flag6)
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    bool flag7 = process.Id != current.Id;
                    if (flag7)
                    {
                        FrameworkAppBase.SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
                Application.Current.Shutdown();
                Environment.Exit(0);
            }
            var loginWindow = Container.Resolve<LoginView>();
            bool? result = loginWindow.ShowDialog();
            // 2️⃣ 登录成功 → 创建主窗口
            if (result == false)
            {
                base.OnInitialized();
            }
            else
            {
                // 登录失败或取消 → 退出程序
                Current.Shutdown();
            }
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册全局依赖，比如主题服务、消息总线等
            containerRegistry.Register<LoginView>();
            containerRegistry.Register<LoginViewModel>();
            containerRegistry.RegisterForNavigation<ShellWindow>();

            #region 日志模块
            // 1. 初始化 Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,   // 按天分文件
                    retainedFileCountLimit: 10,            // 保留10天
                    outputTemplate:
                    "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            // 2. 接入微软日志抽象
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });

            // 3. 注册到 Prism 容器
            containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
            #endregion


        }

        /// <summary>
        /// TODO 1. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

    }
}
