using Framework.Alarm;
using Framework.Alarm.Domain;
using UI.FrameWork.Core.Main;

namespace Framework.Alarm.UI
{
    /// <summary>
    /// 顶栏桥：订阅报警事件流，把活跃快照的汇总状态搬进页头（规则见词汇表「顶栏汇总状态」）。
    /// 壳（UI.FrameWork.Core）不认识报警，家族认识壳的中性顶栏模型（HeaderAlarmLevel 三件套）——
    /// 翻译活在家族里，接线随模块装即通（AlarmUIModule.OnInitialized），宿主不用写一行报警代码。
    /// 事件在引擎线程上到达，读快照与写页头都编队回 UI 线程（摘要 §9.2 的同一规矩：引擎锁内同步播报，
    /// 订阅方自己编队）。<see cref="Connect"/> 补一次初始快照，模块装载时页头不空窗。
    /// </summary>
    public sealed class AlarmHeaderBridge : IDisposable
    {
        /// <summary>馈送接口：事件流与活跃快照的唯一通道（不认识引擎具体类，测试可喂假 feed）。</summary>
        private readonly IAlarmFeed _feed;

        /// <summary>页头 VM：三件套的落点（容器单例，与视图绑定同实例）。</summary>
        private readonly HeaderViewModel _header;

        /// <summary>UI 上下文：快照重算与页头写入都编队回这里。</summary>
        private readonly SynchronizationContext _ui;

        /// <summary>读取时刻：剔除搁置到期成员用（测试注入固定值控制时间）。</summary>
        private readonly Func<DateTimeOffset> _now;

        /// <summary>构造即订阅事件流；Connect 补初始快照。</summary>
        public AlarmHeaderBridge(IAlarmFeed feed, HeaderViewModel header, Func<DateTimeOffset>? now = null)
        {
            _feed = feed;
            _header = header;
            _now = now ?? (() => DateTimeOffset.Now);
            _ui = SynchronizationContext.Current ?? new SynchronizationContext();
            _feed.EventRaised += OnEventRaised;
        }

        /// <summary>接通：补一次初始快照（模块装载时活跃列表可能已经有账，页头别空窗）。</summary>
        public void Connect()
        {
            var summary = AlarmHeaderStatus.Summarize(_feed.Active, _now());
            _ui.Post(_ => Apply(summary), null);
        }

        /// <summary>每条事件都全量重算汇总：规则只有一个出口（Summarize），不搞增量维护。</summary>
        private void OnEventRaised(AlarmEvent e) =>
            _ui.Post(_ => Apply(AlarmHeaderStatus.Summarize(_feed.Active, _now())), null);

        /// <summary>把三件套写进页头（UI 线程）。</summary>
        private void Apply(AlarmHeaderStatus.Summary summary)
        {
            _header.AlarmLevel = summary.Level;
            _header.AlarmCount = summary.Count;
            _header.AlarmText = summary.Text;
        }

        /// <summary>收尾：退订事件流（桥随应用存亡，规范起见随宿主一起处置）。</summary>
        public void Dispose()
        {
            _feed.EventRaised -= OnEventRaised;
        }
    }
}
