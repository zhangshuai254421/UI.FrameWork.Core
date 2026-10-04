namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 会抖的存储替身（#31）：前 <see cref="_failures"/> 次 Append 一律炸（模拟 busy 超时/磁盘抖动），
    /// 之后放行——重试契约的考官；<c>int.MaxValue</c> 即每试必炸（库彻底坏掉的替身）。
    /// <see cref="Attempts"/> 记出手次数，<see cref="Landed"/> 只记走完的。
    /// </summary>
    internal sealed class FlakyEventStore : IEventStore
    {
        private readonly int _failures;

        public FlakyEventStore(int failures) => _failures = failures;

        /// <summary>出手次数：含失败的尝试（重试有没有真发生，看它）。</summary>
        public int Attempts { get; private set; }

        /// <summary>真正落进本子的账：成功那次才算。</summary>
        public List<AlarmEvent> Landed { get; } = [];

        /// <summary>首次落定信号：测试等它即「重试到成功」，不猜钟。</summary>
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
