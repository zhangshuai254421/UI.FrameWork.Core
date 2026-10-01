using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using EFCore.Infrastructure;
using EFCore.Repository;
using Example;
using Framework.Core.Common;
using Framework.Device.Domain;
using Framework.Device.Infrastructure;
using Log.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrismUI.Core;
using Recipe.Domain;
using Recipe.Infrastructure;
using Serilog;
using Log.Infrastructure;
using Serilog.Sinks.RichTextBox.Themes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using UI.FrameWork.Core.Common;
using UI.FrameWork.Core.Main;
using UI.FrameWork.Core.Main.Footer;

namespace UI.FrameWork.Core
{

    public abstract class FrameworkAppBase : PrismApplicationBase
    {
        #region 字段

        private readonly ServiceCollection _services = new ServiceCollection();
        private static Mutex AppMutex;
        private DeviceManager? _deviceManager;

        #endregion

        #region 方法

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override IContainerExtension CreateContainerExtension()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddDbContext<Recipe.Infrastructure.DataContext>();
            services.AddDbContext<Log.Infrastructure.DataContext>();
            services.AddDbContext<Framework.Device.Infrastructure.DeviceDataContext>();
            /// TODO 2. 注册数据访问层（DAL）和业务逻辑层（BLL）的服务
            services.AddRepository();

            services.AddSerilogServices();
            services.AddRecipeServices();
            services.AddDeviceServices();

            
            services.AddSingleton<INavigationService, PrismUI.Core.NavigationService>();

           

            return new DryIocContainerExtension(new DryIoc.Container(DryIocContainerExtension.DefaultRules) .WithDependencyInjectionAdapter(services));
        }

        protected override Window CreateShell()
        {
            AssemblyPreloader.PreloadPluginAssemblies(AppDomain.CurrentDomain.BaseDirectory);

            Container.Resolve<IServiceProvider>().UseDatabaseEnsureCreated<Recipe.Infrastructure.DataContext>();
            Container.Resolve<IServiceProvider>().UseDatabaseEnsureCreated<Log.Infrastructure.DataContext>();
            Container.Resolve<IServiceProvider>().UseDatabaseEnsureCreated<Framework.Device.Infrastructure.DeviceDataContext>();

          
            return Container.Resolve<ShellWindow>(); ;
        }

        /// <summary>
        /// TODO 3
        /// </summary>
        protected override void OnInitialized()
        {
            IoC.GetInstance = this.Container.Resolve;
            //IoC.BuildUp = this.Container.BuildUp;

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
                // 初始化硬件设备管理器（同步等待，避免阻塞UI线程过久）
                try
                {
                    _deviceManager = Container.Resolve<DeviceManager>();

                    // 配置读取器为作用域服务（依赖 DbContext），初始化完成后即释放。
                    using var scope = Container.Resolve<IServiceProvider>().CreateScope();
                    var reader = scope.ServiceProvider.GetRequiredService<IDeviceConfigurationReader>();
                    _deviceManager.Initialize(reader);
                }
                catch (Exception ex)
                {
                    var logger = Container.Resolve<ILogger<FrameworkAppBase>>();
                    logger?.LogError(ex, "设备管理器初始化失败");
                }

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
            containerRegistry.Register<ShellWindow>();

            #region 日志模块
            // 1. 初始化 Serilog
            //Serilog.Log.Logger = new LoggerConfiguration()
            //     .Enrich.WithMachineName()             // 添加 MachineName
            //     .Enrich.WithThreadId()                // 添加 ThreadId
            //     .Enrich.WithEnvironmentUserName()     // 添加当前用户名
            //    .MinimumLevel.Debug()
            //    .WriteTo.SQLite(AppGlobals.LogDbFilePathNoDebug, tableName: "SerilogHistory")  // 指定数据库文件路径
            //    .WriteTo.File(
            //        "logs/log-.txt",
            //        rollingInterval: RollingInterval.Day,   // 按天分文件
            //        retainedFileCountLimit: 10,            // 保留10天
            //        outputTemplate:
            //        "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            //    )
            //    //.WriteTo.RichTextBox(LogShowView.Instance, theme: RichTextBoxConsoleTheme.Colored)
            //    .CreateLogger();

            //// 2. 接入微软日志抽象
            //var loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    builder.AddSerilog();
            //});

            //// 3. 注册到 Prism 容器
            //containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);
            //containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));

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

        protected override void OnExit(ExitEventArgs e)
        {
            // 关闭所有设备
            try
            {
                _deviceManager?.Dispose();
            }
            catch
            {
                // 忽略退出时的异常
            }

            //host.StopAsync().Wait();
            //host.Dispose();
            base.OnExit(e);
        }

        #endregion
    }
}
