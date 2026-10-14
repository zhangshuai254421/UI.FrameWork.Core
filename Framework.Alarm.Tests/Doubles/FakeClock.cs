namespace Framework.Alarm.Tests
{
    /// <summary>时钟替身：手动控制时间，不真等待（ADR-0009 规则 6）。</summary>
    internal sealed class FakeClock : IAlarmClock
    {
        public DateTimeOffset Now { get; private set; }

        public FakeClock(DateTimeOffset now) => Now = now;

        /// <summary>把时钟向前拨。</summary>
        public void Advance(TimeSpan by) => Now += by;
    }
}
