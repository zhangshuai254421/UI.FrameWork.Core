using Framework.Alarm.Domain;
using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 报警模块的依赖注入注册入口（#22 家族组装接线）：两个存储注册为单例——
    /// 引擎启动要全量加载档案，EventHistory 是长命接线，存储随应用存亡。
    /// 连接模型（#22 拍板）：两个存储各开连接访问同一个库文件（连接字符串选项，不共享连接实例），
    /// 跨连接并发由 AlarmDatabase.Initialize 开的 WAL 兜住；建库同样统一在宿主启动时
    /// （AlarmDatabase.Initialize），存储构造不建库。
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>注册报警存储：库选项（用应用全局路径）与档案、事件两个存储，全部单例。</summary>
        public static IServiceCollection AddAlarmServices(this IServiceCollection services)
        {
            services.AddSingleton(_ =>
                new DbContextOptionsBuilder<DataContext>()
                    .UseSqlite(AppGlobals.AlarmDbFliePath)
                    .Options);
            services.AddSingleton<IAlarmDefinitionStore, AlarmDefinitionStore>();
            services.AddSingleton<IEventStore, EventStore>();
            return services;
        }
    }
}
