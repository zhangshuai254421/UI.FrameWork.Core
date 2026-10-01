using Framework.Core.Common;
using Log.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Infrastructure
{
    /// <summary>
    /// 日志模块依赖注入扩展：注册日志服务，并初始化 Serilog（SQLite 落库加按天滚动文件）。
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>注册日志服务，创建 Serilog 全局日志器并接入微软日志抽象（ILoggerFactory 与 ILogger）。</summary>
        public static IServiceCollection AddSerilogServices(this IServiceCollection services)
        {
            services.AddScoped<ISerilogService, SerilogService>();


            #region 日志模块
            // 1. 初始化 Serilog
            Serilog.Log.Logger = new LoggerConfiguration()
                 .Enrich.WithMachineName()             // 添加 MachineName
                 .Enrich.WithThreadId()                // 添加 ThreadId
                 .Enrich.WithEnvironmentUserName()     // 添加当前用户名
                .MinimumLevel.Debug()
                .WriteTo.SQLite(AppGlobals.LogDbFilePathNoDebug, tableName: "SerilogHistory")  // 指定数据库文件路径
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
            services.AddSingleton<ILoggerFactory>(loggerFactory);
            services.AddScoped(typeof(ILogger<>), typeof(Logger<>));

            #endregion


            return services;
        }
    }
}
