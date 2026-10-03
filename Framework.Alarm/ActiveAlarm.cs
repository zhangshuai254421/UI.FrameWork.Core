namespace Framework.Alarm
{
    /// <summary>
    /// 活跃列表成员（摘要 §8.9）：引擎的只读投影——当前挂着未确认 + 已确认 + RTN 的报警；
    /// 是事件流的投影，不是第二份真相（CONTEXT.md「活跃列表」，_Avoid_：报警缓存、当前报警表）。
    /// </summary>
    /// <param name="Tag">位号。</param>
    /// <param name="State">报警状态：三态样式绑它，不绑数值差（哲学 §3.2【强制】）。</param>
    /// <param name="Priority">优先级：来自档案，排序置顶按它。</param>
    /// <param name="Since">活跃起点：本次升级时刻（同一轮报警的出生时间，恢复再触发则刷新）。</param>
    /// <param name="ShelvedUntil">搁置标（摘要 §8.10）：期限还在当下之后则带标（快照读取时对表判定），
    /// 过期即 null——主列表按它过滤、搁置清单 = 带标成员，时钟一过标自然失效，无定时器。</param>
    public sealed record ActiveAlarm(string Tag, AlarmState State, AlarmPriority Priority, DateTimeOffset Since, DateTimeOffset? ShelvedUntil = null);
}
