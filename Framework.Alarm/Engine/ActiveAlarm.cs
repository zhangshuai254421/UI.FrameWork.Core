namespace Framework.Alarm
{
    /// <summary>
    /// 活跃列表成员（摘要 §8.9）：引擎的只读快照，当前挂着未确认、已确认和 RTN 的报警。
    /// 它是事件流的投影，不是第二份真相（CONTEXT.md「活跃列表」，_Avoid_：报警缓存、当前报警表）。
    /// </summary>
    /// <param name="Tag">位号。</param>
    /// <param name="State">报警状态：界面三态样式绑定它，不要按数值差计算（哲学 §3.2【强制】）。</param>
    /// <param name="Priority">优先级：来自档案，置顶排序按它。</param>
    /// <param name="Since">活跃起点：本次升级的时刻（这一轮报警的开始时间，恢复后再次触发则刷新）。</param>
    /// <param name="ShelvedUntil">搁置期限（摘要 §8.10）：还没到期就有值，已到期为 null
    /// （读取快照时用当前时刻判定）。主列表按它过滤，搁置清单就是有期限的成员；
    /// 到期不需要定时器，比较时刻自然失效。</param>
    public sealed record ActiveAlarm(string Tag, AlarmState State, AlarmPriority Priority, DateTimeOffset Since, DateTimeOffset? ShelvedUntil = null);
}
