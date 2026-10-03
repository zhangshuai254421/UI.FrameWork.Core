namespace Framework.Alarm
{
    /// <summary>
    /// 报警馈送接缝（摘要 §8.4）：呈现层（与历史库）认接口不认引擎——ViewModel 对本接口写测试，
    /// fake 一个 feed 即可红绿灯，谁在生产事件、谁在消费事件互不相认。
    /// <see cref="AlarmEngine"/> 是 v1 唯一实现。
    /// 形状即摘要 §8.4 的全量五员：事件流、活跃列表、确认、搁置、解除——操作成员随搁置工单（#14）
    /// 落地，接缝不再收窄；呈现层的跨线程编队、按标过滤是订阅方自己的事（摘要 §9.2、§8.10）。
    /// </summary>
    public interface IAlarmFeed
    {
        /// <summary>事件流：引擎产出的每一拍（append-only 落库、推 UI 的挂钩，摘要 §8.11）。</summary>
        event Action<AlarmEvent>? EventRaised;

        /// <summary>活跃列表只读投影：成员 = 未确认 + 已确认 + RTN，快照带搁置标（摘要 §8.9/§8.10）；
        /// 主列表/搁置清单 = <see cref="AlarmFilters"/> 对本投影的两个视图。</summary>
        IReadOnlyList<ActiveAlarm> Active { get; }

        /// <summary>操作员确认：未确认 → 已确认；RTN → 确认恢复闭环（摘要 §8.4）。</summary>
        void Acknowledge(string tag, string op);

        /// <summary>操作员搁置：事件打标不拦状态机；期限必带（无限期搁置 = 变相删报警，哲学 §7）。</summary>
        void Shelve(string tag, TimeSpan duration, string op);

        /// <summary>操作员解除搁置：撤标，回主列表正常播报。</summary>
        void Unshelve(string tag, string op);
    }
}
