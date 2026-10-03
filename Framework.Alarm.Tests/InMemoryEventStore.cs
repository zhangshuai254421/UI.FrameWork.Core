using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>事件存储内存替身：契约测试与引擎联测用，行为与 SQLite 实现同一套契约。</summary>
    internal sealed class InMemoryEventStore : IEventStore
    {
        private readonly List<AlarmEvent> _events = new();

        public void Append(AlarmEvent e) => _events.Add(e);

        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null)
            => _events
                .Where(e => e.At >= from && e.At <= to)
                .Where(e => tag == null || e.Tag == tag)
                .ToList(); // 落库先后即履历次序，不重排
    }
}
