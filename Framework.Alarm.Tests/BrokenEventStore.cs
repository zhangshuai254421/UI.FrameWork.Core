namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 治不好的存储替身（#31）：Append 每试必炸、当场炸（不等 30 秒 busy 窗口）——
    /// 模拟库彻底坏掉（磁盘拔了/schema 炸了）；「收工让位」契约的考官。
    /// </summary>
    internal sealed class BrokenEventStore : IEventStore
    {
        /// <summary>真正落进本子的账（在本替身身上永远空着）。</summary>
        public List<AlarmEvent> Landed { get; } = [];

        public void Append(AlarmEvent e) => throw new IOException("库彻底进不去（模拟磁盘故障）");

        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
            => Landed;
    }
}
