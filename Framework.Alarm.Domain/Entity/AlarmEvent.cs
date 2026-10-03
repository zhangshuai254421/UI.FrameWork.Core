namespace Framework.Alarm.Domain
{
    /// <summary>
    /// 报警事件族（摘要 §8.5）：事件就是报警的履历，引擎产出的全量落历史库（append-only）。
    /// 铁律：<see cref="AlarmAutoReturned"/> 与 <see cref="AlarmClosed"/> 必须是两种事件——
    /// 无人看见的恢复 ≠ 真闭环，早班的「昨夜发生过什么」靠这条区别（哲学 §3.5【强制】）。
    /// 时间口径：条件驱动的事件（升级/清除）带条件事实的时间戳，操作员驱动的事件（确认/闭环/搁置/解除）
    /// 带时钟接缝的当下（ADR-0009 规则 6）。
    /// #20 实体转正：事件住家族模型中心（ADR-0008 家族形态）。历史一拍一横锤、永不改写——
    /// record 不可变正是 append-only 的类型级表达；值相等契约是往返「原样读回」的断言依据。
    /// EF 映射（#21）走构造绑定 + TPH 判别，履历次序（自增列）由持久化层配置。
    /// </summary>
    /// <param name="Tag">位号：哪条报警。</param>
    /// <param name="At">事件时刻。</param>
    public abstract record AlarmEvent(string Tag, DateTimeOffset At);

    /// <summary>升级：越限持续满触发延时，正式报警（闪红起，进活跃列表）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">升级时刻（补满触发延时的那条事实的时间戳）。</param>
    /// <param name="Priority">优先级：来自档案（查矩阵的输出），呈现层按它置顶排序。</param>
    /// <param name="ShelvedUntil">搁置标（摘要 §8.10）：升级发生在搁置中则带标——历史全量记录一拍不缺，
    /// 呈现层按标决定这条进主列表还是只进搁置清单；null = 未搁置（到期后标不随事件走）。</param>
    public sealed record AlarmRaised(string Tag, DateTimeOffset At, AlarmPriority Priority, DateTimeOffset? ShelvedUntil = null) : AlarmEvent(Tag, At);

    /// <summary>操作员确认：责任接管（由闪变稳）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">确认时刻（操作员动作，取时钟接缝当下）。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmAcknowledged(string Tag, DateTimeOffset At, string Operator) : AlarmEvent(Tag, At);

    /// <summary>无人看见的恢复：确认之前条件自愈，进 RTN（闪绿留列表等一个「看见了」）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">清除时刻（补满恢复延时的那条事实的时间戳）。</param>
    public sealed record AlarmAutoReturned(string Tag, DateTimeOffset At) : AlarmEvent(Tag, At);

    /// <summary>真闭环：已确认后条件恢复直接闭环，或 RTN 等到了那个「看见了」；移出活跃列表。
    /// 不密封：操作员确认闭环（<see cref="AlarmClosedByOperator"/>，#26）由它派生——
    /// 消费方的类型匹配照旧吃派生型，闭环语义一处定义。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">闭环时刻（条件恢复取事实时间戳，RTN 确认取时钟接缝当下）。</param>
    public record AlarmClosed(string Tag, DateTimeOffset At) : AlarmEvent(Tag, At);

    /// <summary>操作员确认闭环（#26）：RTN 等到了那个「看见了」，由操作员亲手关账。与条件驱动的
    /// <see cref="AlarmClosed"/> 分型——「无人看见的恢复后闭环」与「操作员确认的闭环」在本子上可分，
    /// 审计答得出「这条 RTN 是谁确认闭环的」（操作员动作全量留痕，哲学 §7【强制】）。
    /// 派生自 AlarmClosed：消费方的类型匹配（呈现层 Closed 移出列表等）照旧吃它，闭环语义不变。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">闭环时刻（操作员动作，取时钟接缝当下）。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmClosedByOperator(string Tag, DateTimeOffset At, string Operator) : AlarmClosed(Tag, At);

    /// <summary>操作员搁置（摘要 §8.10）：合法闭嘴的正门——报警该乱报的照常走状态机，只是带标退到搁置清单。
    /// 期限必带（无限期搁置 = 变相删报警，哲学 §7【强制】），时刻取时钟接缝当下，操作员全量留痕。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">搁置时刻（操作员动作，取时钟接缝当下）。</param>
    /// <param name="ShelvedUntil">搁置期限：到点标自然失效，无定时器解除。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmShelved(string Tag, DateTimeOffset At, DateTimeOffset ShelvedUntil, string Operator) : AlarmEvent(Tag, At);

    /// <summary>操作员解除搁置：撤标，报警回主列表正常播报；时刻取时钟接缝当下，操作员全量留痕。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">解除时刻（操作员动作，取时钟接缝当下）。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmUnshelved(string Tag, DateTimeOffset At, string Operator) : AlarmEvent(Tag, At);
}
