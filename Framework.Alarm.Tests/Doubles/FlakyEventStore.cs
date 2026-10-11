namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 会失败的存储替身（#31）：前 <see cref="_failures"/> 次 Append 一律抛异常（模拟 busy 超时/磁盘抖动），
    /// 之后放行——重试契约的考官；传 <c>int.MaxValue</c> 即每次都失败（库彻底坏掉的替身）。
    /// <see cref="Attempts"/> 记尝试次数，<see cref="Landed"/> 只记成功的。
    /// </summary>
    internal sealed class FlakyEventStore : IEventStore
    {
        private readonly int _failures;

        public FlakyEventStore(int failures) => _failures = failures;

        /// <summary>尝试次数：含失败的（重试有没有真发生，看它）。</summary>
        public int Attempts { get; private set; }

        /// <summary>真正写进库的事件：成功那次才算。</summary>
        public List<AlarmEvent> Landed { get; } = [];

        /// <summary>首次成功信号：测试等它即「重试到成功」，不猜时间。</summary>
        public ManualResetEventSlim LandedOnce { get; } = new();

        public void Append(AlarmEvent e)
        {
            Attempts++;
            if (Attempts <= _failures)
            {
                throw new IOException("库暂时进不去（模拟 busy 超时）");
            }

            Landed.Add(e);
            LandedOnce.Set();
        }

        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
            => Landed
                .Where(e => e.At >= from && e.At <= to)
                .Where(e => tag == null || e.Tag == tag)
                .ToList();
    }
}
