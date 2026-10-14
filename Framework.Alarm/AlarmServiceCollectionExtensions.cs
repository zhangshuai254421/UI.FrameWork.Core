using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Framework.Alarm
{
    /// <summary>
    /// 报警引擎的容器注册入口：引擎是长命单例（档案启动时全量加载、事件流随应用产出），
    /// 一个注册覆盖两个契约——具体类 <see cref="AlarmEngine"/>（桥和演示装置要真实例）
    /// 与 <see cref="IAlarmFeed"/>（呈现层和历史库只认接口，摘要 §8.4）。
    /// 时钟有公认默认（墙钟），一并注册；条件源是宿主的事（现场接什么采集层宿主最清楚），
    /// 宿主把真源注册在 <see cref="AddAlarmEngine"/> 之前即生效；没注册就用空源兜底，
    /// 引擎立得住、一声不吭，顶栏不会因为「还没接现场」在启动时炸模块。
    /// </summary>
    public static class AlarmServiceCollectionExtensions
    {
        /// <summary>注册报警引擎（单例）：IAlarmClock 用墙钟默认，IAlarmFeed 转手引擎实例，条件源宿主未注册时空源兜底。</summary>
        public static IServiceCollection AddAlarmEngine(this IServiceCollection services)
        {
            services.AddSingleton<IAlarmClock, SystemClock>();
            services.TryAddSingleton<IConditionSource, EmptyConditionSource>();
            services.AddSingleton<AlarmEngine>();
            services.AddSingleton<IAlarmFeed>(sp => sp.GetRequiredService<AlarmEngine>());
            return services;
        }
    }
}
