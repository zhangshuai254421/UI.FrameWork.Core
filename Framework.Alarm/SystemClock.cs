namespace Framework.Alarm
{
    /// <summary>
    /// 系统墙钟：<see cref="IAlarmClock"/> 的生产实现——宿主接线与演示装置用（引擎等模块代码只认接缝，
    /// 测试注 FakeClock 拨表，ADR-0009 规则 6）。
    /// </summary>
    public sealed class SystemClock : IAlarmClock
    {
        /// <summary>当前时刻 = 本机现在。</summary>
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
