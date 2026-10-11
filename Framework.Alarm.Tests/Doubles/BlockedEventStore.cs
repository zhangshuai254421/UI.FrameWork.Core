namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 会堵住的存储替身（#31）：Append 进门先报告、再卡住等待——模拟库文件被外部进程独占
    /// （备份/杀毒，#29 场景）时 busy 等待不返回的那段时间；放行之前不写库，
    /// <see cref="Landed"/> 只记走完全程的——「进了 Append」和「写进库」两件事分得开。
    /// </summary>
    internal sealed class BlockedEventStore : IEventStore
    {
        /// <summary>写者已进 Append 的信号（卡住前点亮）。</summary>
        public ManualResetEventSlim Entered { get; } = new();

        /// <summary>放行信号：Set 之前 Append 一律不返回——外部锁住库文件的那段时间。</summary>
        public ManualResetEventSlim Gate { get; } = new();

        /// <summary>真正写进库的事件：Append 走完才算。</summary>
        public List<AlarmEvent> Landed { get; } = [];

        public void Append(AlarmEvent e)
        {
            Entered.Set();
            Gate.Wait();
            Landed.Add(e);
        }

        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
            => Landed
                .Where(e => e.At >= from && e.At <= to)
                .Where(e => tag == null || e.Tag == tag)
                .ToList();
    }
}
