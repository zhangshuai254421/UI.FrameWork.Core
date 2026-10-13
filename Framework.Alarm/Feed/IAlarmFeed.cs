namespace Framework.Alarm
{
    /// <summary>
    /// 报警馈送接口（摘要 §8.4）：呈现层和历史库只认接口不认引擎。ViewModel 针对本接口写测试，
    /// 造一个假的 feed 就能跑，谁在生产事件、谁在消费事件互不依赖。
    /// <see cref="AlarmEngine"/> 是 v1 的唯一实现。
    /// 接口成员就是摘要 §8.4 的全部五项加 IDisposable：事件流、活跃列表、确认、搁置、解除——
    /// 搁置等操作成员随搁置工单（#14）落地后，接口不再收窄。跨线程投递、按期限过滤都是订阅方自己的事
    /// （摘要 §9.2、§8.10）。
    /// IDisposable 随接口提供：换档案重建引擎时，宿主经本接口处置旧引擎（退订采集层，避免双引擎重复发事件）。
    /// </summary>
    public interface IAlarmFeed : IDisposable
    {
        /// <summary>事件流：引擎产出的每条事件（落历史库、推 UI 的挂钩，摘要 §8.11）。</summary>
        event Action<AlarmEvent>? EventRaised;

        /// <summary>活跃列表只读快照：成员是未确认、已确认和 RTN 的报警，快照带搁置期限（摘要 §8.9/§8.10）；
        /// 主列表和搁置清单 = <see cref="AlarmFilters"/> 对这份快照的两个视图。</summary>
        IReadOnlyList<ActiveAlarm> Active { get; }

        /// <summary>操作员确认：未确认 → 已确认；RTN → 确认恢复闭环（摘要 §8.4）。</summary>
        void Acknowledge(string tag, string op);

        /// <summary>操作员搁置：只附加期限，不阻止状态机；期限必填（无限期搁置 = 变相删报警，哲学 §7）。</summary>
        void Shelve(string tag, TimeSpan duration, string op);

        /// <summary>操作员解除搁置：取消期限，报警回到主列表正常显示。</summary>
        void Unshelve(string tag, string op);
    }
}
