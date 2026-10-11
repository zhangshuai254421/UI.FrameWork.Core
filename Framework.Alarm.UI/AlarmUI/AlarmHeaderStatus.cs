using Framework.Alarm;
using Framework.Alarm.Domain;
using UI.FrameWork.Core.Main;

namespace Framework.Alarm.UI
{
    /// <summary>
    /// 顶栏汇总状态的映射规则（词汇表「顶栏汇总状态」）：把活跃快照浓缩成顶栏按钮的三件套（级别/计数/文本）。
    /// 规矩：搁置中的成员不参与汇总。搁置是限时闭嘴（哲学 §7），顶栏不替它喊。
    /// 有未确认按优先级分档：含中/高优先级 → Alarm 红闪，仅低优先级 → Warning 黄闪。
    /// 无未确认但有已确认挂着 → AlarmAcknowledged 稳红，责任接管了但还没闭环。
    /// 只有 RTN → Solved 橙稳，等一个「看见了」。全无 → Normal。
    /// 计数 = 未搁置的活跃行数。文本 = 最高优先级未确认的位号；没有未确认时取最高优先级活跃成员的位号。
    /// 纯函数、线程无关：活跃快照随时可重算，规则修订只动这一个类。
    /// </summary>
    public static class AlarmHeaderStatus
    {
        /// <summary>顶栏三件套：级别、计数、文本。桥只负责搬运，规则全在这里。</summary>
        public readonly record struct Summary(HeaderAlarmLevel Level, int Count, string Text);

        /// <summary>活跃快照 → 顶栏三件套。now 用来剔除搁置已到期的成员（测试注入固定时刻）。</summary>
        public static Summary Summarize(IReadOnlyList<ActiveAlarm> active, DateTimeOffset now)
        {
            // 搁置中的成员不参与汇总：期限没到的才算「闭嘴中」（引擎快照已把到期的置回 null）。
            var live = active.Where(a => a.ShelvedUntil is null || a.ShelvedUntil > now).ToList();

            var unack = live.Where(a => a.State == AlarmState.Unacknowledged).ToList();

            if (live.Count == 0)
            {
                return new Summary(HeaderAlarmLevel.Normal, 0, string.Empty);
            }

            if (unack.Count > 0)
            {
                // 有未确认：含中/高优先级 → 红闪；仅低 → 黄闪。文本给最高优先级那条的位号。
                var hasMediumOrHigh = unack.Any(a => a.Priority is AlarmPriority.High or AlarmPriority.Medium);
                var top = unack.OrderBy(a => Rank(a.Priority)).First();
                var level = hasMediumOrHigh ? HeaderAlarmLevel.Alarm : HeaderAlarmLevel.Warning;
                return new Summary(level, live.Count, top.Tag);
            }

            // 无未确认：已确认还挂着 → 稳红；只剩 RTN → 橙稳。文本给最高优先级活跃成员的位号。
            var stillHasAcknowledged = live.Any(a => a.State == AlarmState.Acknowledged);
            var topLive = live.OrderBy(a => Rank(a.Priority)).First();
            var steadyLevel = stillHasAcknowledged ? HeaderAlarmLevel.AlarmAcknowledged : HeaderAlarmLevel.Solved;
            return new Summary(steadyLevel, live.Count, topLive.Tag);
        }

        /// <summary>优先级权重（汇总分档用）：高 0 / 中 1 / 低 2，数越小越代表顶栏。</summary>
        private static int Rank(AlarmPriority priority) => priority switch
        {
            AlarmPriority.High => 0,
            AlarmPriority.Medium => 1,
            _ => 2,
        };
    }
}
