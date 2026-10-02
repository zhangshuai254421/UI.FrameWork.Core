namespace Framework.Alarm
{
    /// <summary>
    /// 报警事件族（摘要 §8.5）：事件就是报警的履历，引擎产出的全量落历史库（append-only）。
    /// 铁律：<see cref="AlarmAutoReturned"/> 与 <see cref="AlarmClosed"/> 必须是两种事件——
    /// 无人看见的恢复 ≠ 真闭环，早班的「昨夜发生过什么」靠这条区别（哲学 §3.5【强制】）。
    /// 时间口径：条件驱动的事件（升级/清除）带条件事实的时间戳，操作员驱动的事件（确认/闭环）
    /// 带时钟接缝的当下（ADR-0009 规则 6）。
    /// </summary>
    /// <param name="Tag">位号：哪条报警。</param>
    /// <param name="At">事件时刻。</param>
    public abstract record AlarmEvent(string Tag, DateTimeOffset At);

    /// <summary>升级：越限持续满触发延时，正式报警（闪红起，进活跃列表）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">升级时刻（补满触发延时的那条事实的时间戳）。</param>
    /// <param name="Priority">优先级：来自档案（查矩阵的输出），呈现层按它置顶排序。</param>
    public sealed record AlarmRaised(string Tag, DateTimeOffset At, AlarmPriority Priority) : AlarmEvent(Tag, At);

    /// <summary>操作员确认：责任接管（由闪变稳）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">确认时刻（操作员动作，取时钟接缝当下）。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmAcknowledged(string Tag, DateTimeOffset At, string Operator) : AlarmEvent(Tag, At);

    /// <summary>无人看见的恢复：确认之前条件自愈，进 RTN（闪绿留列表等一个「看见了」）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">清除时刻（补满恢复延时的那条事实的时间戳）。</param>
    public sealed record AlarmAutoReturned(string Tag, DateTimeOffset At) : AlarmEvent(Tag, At);

    /// <summary>真闭环：已确认后条件恢复直接闭环，或 RTN 等到了那个「看见了」；移出活跃列表。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">闭环时刻（条件恢复取事实时间戳，RTN 确认取时钟接缝当下）。</param>
    public sealed record AlarmClosed(string Tag, DateTimeOffset At) : AlarmEvent(Tag, At);
}
