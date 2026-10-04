namespace Framework.Alarm.Tests
{
    /// <summary>馈送替身：测试直控事件流的拍子（EventHistory 接线契约用）——不架真引擎，
    /// 想吐哪一拍吐哪一拍；动作成员全 no-op（本类只验接线的落库侧）。</summary>
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

        /// <summary>模拟引擎吐事件（测试直控拍点与内容）。</summary>
        public void Emit(AlarmEvent e) => EventRaised?.Invoke(e);
    }
}
