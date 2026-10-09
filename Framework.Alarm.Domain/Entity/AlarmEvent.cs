namespace Framework.Alarm.Domain
{
    /// <summary>
    /// 报警事件族（摘要 §8.5）：事件就是报警的履历，引擎产出的全量写入历史库（append-only）。
    /// 铁律：<see cref="AlarmAutoReturned"/> 与 <see cref="AlarmClosed"/> 必须是两种事件——
    /// 没人看见的恢复不等于真闭环，早班问「昨夜发生过什么」靠这条区别回答（哲学 §3.5【强制】）。
    /// 时间口径：条件驱动的事件（升级/清除）带条件事实的时间戳；操作员驱动的事件
    /// （确认/闭环/搁置/解除）带时钟当前时刻（ADR-0009 规则 6）。
    /// #20 实体转正：事件住家族模型中心（ADR-0008 家族形态）。历史只增不改——
    /// record 不可变正是 append-only 在类型层面的表达；值相等契约是存储往返「原样读回」的断言依据。
    /// EF 映射（#21）走构造绑定 + TPH 判别，履历次序（自增列）由持久化层配置。
    /// </summary>
    /// <param name="Tag">位号：哪条报警。</param>
    /// <param name="At">事件时刻。</param>
    public abstract record AlarmEvent(string Tag, DateTimeOffset At);

    /// <summary>升级：越限持续满触发延时，正式报警（闪红开始，进活跃列表）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">升级时刻（补满触发延时的那条事实的时间戳）。</param>
    /// <param name="Priority">优先级：来自档案（查矩阵的输出），呈现层按它置顶排序。</param>
    /// <param name="ShelvedUntil">搁置期限（摘要 §8.10）：升级发生在搁置中则带上当时还剩的期限——
    /// 历史全量记录一条不缺，呈现层按期限决定这条进主列表还是只进搁置清单；
    /// null 表示未搁置（期限到期后不回填到已落库的事件上）。</param>
    public sealed record AlarmRaised(string Tag, DateTimeOffset At, AlarmPriority Priority, DateTimeOffset? ShelvedUntil = null) : AlarmEvent(Tag, At);

    /// <summary>操作员确认：责任接管（由闪变稳）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">确认时刻（操作员动作，取时钟当前时刻）。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmAcknowledged(string Tag, DateTimeOffset At, string Operator) : AlarmEvent(Tag, At);

    /// <summary>没人看见的恢复：确认之前条件自行恢复，进 RTN（闪绿留在列表等操作员确认）。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">清除时刻（补满恢复延时的那条事实的时间戳）。</param>
    public sealed record AlarmAutoReturned(string Tag, DateTimeOffset At) : AlarmEvent(Tag, At);

    /// <summary>真闭环：已确认后条件恢复直接闭环，或 RTN 等到了操作员确认；报警移出活跃列表。
    /// 不密封：操作员确认闭环（<see cref="AlarmClosedByOperator"/>，#26）由它派生——
    /// 消费方的类型匹配照旧识别派生型，闭环语义只在此处定义一次。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">闭环时刻（条件恢复取事实时间戳，RTN 确认取时钟当前时刻）。</param>
    public record AlarmClosed(string Tag, DateTimeOffset At) : AlarmEvent(Tag, At);

    /// <summary>操作员确认闭环（#26）：RTN 等到了操作员确认，由操作员亲手关闭。与条件驱动的
    /// <see cref="AlarmClosed"/> 分成两种类型——「没人看见的恢复后闭环」和「操作员确认的闭环」
    /// 在历史库里要能区分，审计要能回答「这条 RTN 是谁确认闭环的」（操作员动作全量留痕，哲学 §7【强制】）。
    /// 派生自 AlarmClosed：消费方的类型匹配（呈现层收到 Closed 移出列表等）照旧识别它，闭环语义不变。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">闭环时刻（操作员动作，取时钟当前时刻）。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmClosedByOperator(string Tag, DateTimeOffset At, string Operator) : AlarmClosed(Tag, At);

    /// <summary>操作员搁置（摘要 §8.10）：让报警正当静默的手段——报警该触发的照常走状态机，
    /// 只是带期限退到搁置清单。期限必填（无限期搁置 = 变相删报警，哲学 §7【强制】），
    /// 时刻取时钟当前时刻，操作员全量留痕。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">搁置时刻（操作员动作，取时钟当前时刻）。</param>
    /// <param name="ShelvedUntil">搁置期限：到点自然失效，不需要定时器解除。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmShelved(string Tag, DateTimeOffset At, DateTimeOffset ShelvedUntil, string Operator) : AlarmEvent(Tag, At);

    /// <summary>操作员解除搁置：取消期限，报警回主列表正常显示；时刻取时钟当前时刻，操作员全量留痕。</summary>
    /// <param name="Tag">位号。</param>
    /// <param name="At">解除时刻（操作员动作，取时钟当前时刻）。</param>
    /// <param name="Operator">操作员：动作全量留痕。</param>
    public sealed record AlarmUnshelved(string Tag, DateTimeOffset At, string Operator) : AlarmEvent(Tag, At);
}
