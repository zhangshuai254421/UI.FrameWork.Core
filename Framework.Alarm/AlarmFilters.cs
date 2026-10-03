namespace Framework.Alarm
{
    /// <summary>
    /// 活跃列表的两个呈现视图（摘要 §8.10，哲学 §7/§3.2【强制】）：主列表与搁置清单是同一份
    /// <see cref="IAlarmFeed.Active"/> 投影的两个过滤器，不是两张表（警示 8：另建搁置表 = 第二份真相，必对不上账）。
    /// 过滤规则住在引擎库、UI 只调用不判定（UI 铁律：每条呈现规则必须来自引擎产出）——
    /// 引擎不撒谎（全量记录），过滤只看 <see cref="ActiveAlarm.ShelvedUntil"/> 对不对得上现在。
    /// </summary>
    public static class AlarmFilters
    {
        /// <summary>主列表：不带标（或标已过期）的成员——搁置中的报警按标滤出，正常播报。</summary>
        /// <param name="active">活跃列表投影（<see cref="IAlarmFeed.Active"/>）。</param>
        /// <param name="now">读取时刻：与成员的搁置期限对表。</param>
        public static IReadOnlyList<ActiveAlarm> MainList(IEnumerable<ActiveAlarm> active, DateTimeOffset now)
            => active.Where(a => a.ShelvedUntil is null || a.ShelvedUntil <= now).ToList();

        /// <summary>搁置清单：带标且期限未过的成员——「搁置清单单独可见」在这一视图兑现
        /// （哲学 §7；呈现层工单的搁置清单页数据就是它）。</summary>
        /// <param name="active">活跃列表投影（<see cref="IAlarmFeed.Active"/>）。</param>
        /// <param name="now">读取时刻：与成员的搁置期限对表，时钟一过标自然失效。</param>
        public static IReadOnlyList<ActiveAlarm> Shelved(IEnumerable<ActiveAlarm> active, DateTimeOffset now)
            => active.Where(a => a.ShelvedUntil is { } until && until > now).ToList();
    }
}
