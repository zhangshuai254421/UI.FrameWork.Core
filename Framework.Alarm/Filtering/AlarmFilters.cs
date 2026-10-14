namespace Framework.Alarm
{
    /// <summary>
    /// 活跃列表的两个显示视图（摘要 §8.10，哲学 §7/§3.2【强制】）：主列表和搁置清单是
    /// 同一份 <see cref="IAlarmFeed.Active"/> 快照的两个过滤器，不是两张表
    /// （警示 8：另建搁置表等于第二份真相，两边必然对不上）。
    /// 过滤规则放在引擎库里，UI 只调用不自行判定（UI 铁律：每条显示规则必须来自引擎产出）。
    /// 引擎记录全量事实，过滤只比较 <see cref="ActiveAlarm.ShelvedUntil"/> 和当前时刻。
    /// 排序权重 <see cref="Rank"/> 也收在这里：列表排序和顶栏选「最重一条」用的是同一条「高优先级最重」。
    /// </summary>
    public static class AlarmFilters
    {
        /// <summary>主列表：没有搁置期限、或期限已过的成员。搁置中的报警被滤出，其余正常显示。</summary>
        /// <param name="active">活跃列表快照（<see cref="IAlarmFeed.Active"/>）。</param>
        /// <param name="now">读取时刻，用来和成员的搁置期限比较。</param>
        public static IReadOnlyList<ActiveAlarm> MainList(IEnumerable<ActiveAlarm> active, DateTimeOffset now)
            => active.Where(a => a.ShelvedUntil is null || a.ShelvedUntil <= now).ToList();

        /// <summary>搁置清单：有搁置期限且还没到期的成员。「搁置清单单独可见」由这个视图兑现
        /// （哲学 §7；呈现层工单的搁置清单页数据就是它）。</summary>
        /// <param name="active">活跃列表快照（<see cref="IAlarmFeed.Active"/>）。</param>
        /// <param name="now">读取时刻，用来和成员的搁置期限比较；期限一过成员自然回到主列表。</param>
        public static IReadOnlyList<ActiveAlarm> Shelved(IEnumerable<ActiveAlarm> active, DateTimeOffset now)
            => active.Where(a => a.ShelvedUntil is { } until && until > now).ToList();

        /// <summary>优先级排序权重：高 0 / 中 1 / 低 2，数小的排前面（高优先级在顶）。
        /// 「哪个最重」是显示规则，只此一份——列表排序和顶栏文本都从这里取，改规则不会漏一边。</summary>
        public static int Rank(AlarmPriority priority) => priority switch
        {
            AlarmPriority.High => 0,
            AlarmPriority.Medium => 1,
            _ => 2,
        };
    }
}
