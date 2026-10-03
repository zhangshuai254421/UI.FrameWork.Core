using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>事件存储内存替身：契约测试与引擎联测用，行为与 SQLite 实现同一套契约。</summary>
    internal sealed class InMemoryEventStore : IEventStore
    {
        /// <summary>已知判别册（与 SQLite 实现同一份点名簿）：簿外种类当庭退回，不悄悄回空账。</summary>
        private static readonly HashSet<Type> KnownKinds =
        [
            typeof(AlarmRaised), typeof(AlarmAcknowledged), typeof(AlarmAutoReturned),
            typeof(AlarmClosed), typeof(AlarmClosedByOperator), typeof(AlarmShelved), typeof(AlarmUnshelved),
        ];

        private readonly List<AlarmEvent> _events = new();

        public void Append(AlarmEvent e) => _events.Add(e);

        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
        {
            if (kinds is { Length: > 0 } && kinds.Any(k => !KnownKinds.Contains(k)))
            {
                throw new NotSupportedException($"未知事件类型：{string.Join(", ", kinds.Select(k => k.Name))}");
            }

            return _events
                .Where(e => e.At >= from && e.At <= to)
                .Where(e => tag == null || e.Tag == tag)
                .Where(e => kinds is not { Length: > 0 } || kinds.Contains(e.GetType()))
                .ToList(); // 落库先后即履历次序，不重排
        }
    }
}
