using Framework.Alarm.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Alarm
{
    /// <summary>
    /// 报警引擎的容器注册入口：引擎是长命单例（档案启动时全量加载、事件流随应用产出），
    /// 一个注册覆盖两个契约——具体类 <see cref="AlarmEngine"/>（桥和演示装置要真实例）
    /// 与 <see cref="IAlarmFeed"/>（呈现层和历史库只认接口，摘要 §8.4）。
    /// 时钟有公认默认（墙钟），一并注册；条件源没有默认——现场接什么采集层是宿主的事，
    /// 宿主必须先注册 <see cref="IConditionSource"/>，引擎解析才立得起来。
    /// </summary>
    public static class AlarmServiceCollectionExtensions
    {
        /// <summary>注册报警引擎（单例）：IAlarmClock 用墙钟默认，IAlarmFeed 转手引擎实例。</summary>
        public static IServiceCollection AddAlarmEngine(this IServiceCollection services)
        {
            services.AddSingleton<IAlarmClock, SystemClock>();
            services.AddSingleton<AlarmEngine>();
            services.AddSingleton<IAlarmFeed>(sp => sp.GetRequiredService<AlarmEngine>());
            return services;
        }
    }
}
