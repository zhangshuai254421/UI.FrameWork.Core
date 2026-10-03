using Framework.Alarm.Domain;
using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 报警模块的依赖注入注册入口（#22 家族组装接线）：两存储注册为单例——
    /// 引擎启动全量装档案、EventHistory 长命接线，存储随应用存亡。
    /// 连接模型（#22 拍板）：两存储各开连接打同一库文件（连接字符串选项，不共享连接实例），
    /// 跨连接并发由 AlarmDatabase.Initialize 的 WAL 拍板兜住；建库同样归宿主启动一口
    /// （AlarmDatabase.Initialize），存储构造不建库。
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>注册报警存储：库选项（落应用全局路径）与档案/事件两存储，全单例。</summary>
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
