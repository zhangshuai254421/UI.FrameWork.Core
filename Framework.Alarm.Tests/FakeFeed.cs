namespace Framework.Alarm.Tests
{
    /// <summary>报警事件的测试替身，EventHistory 接线契约用。不架真引擎，测试想发哪条就发哪条；
    /// 操作成员全部空实现，因为本类只验接线中落库这一侧。</summary>
    internal sealed class FakeFeed : IAlarmFeed
    {
        public event Action<AlarmEvent>? EventRaised;

        public IReadOnlyList<ActiveAlarm> Active { get; } = [];

        public void Acknowledge(string tag, string op)
        {
        }

        public void Shelve(string tag, TimeSpan duration, string op)
        {
        }

        public void Unshelve(string tag, string op)
        {
        }

        public void Dispose()
        {
        }

        /// <summary>模拟引擎发事件（测试直接控制时机与内容）。</summary>
        public void Emit(AlarmEvent e) => EventRaised?.Invoke(e);
    }
}
