namespace Framework.Alarm
{
    /// <summary>
    /// 系统墙钟：<see cref="IAlarmClock"/> 的生产实现，宿主接线和演示装置用
    /// （引擎等模块代码只认接口；测试注入 FakeClock 控制时间，ADR-0009 规则 6）。
    /// </summary>
    public sealed class SystemClock : IAlarmClock
    {
        /// <summary>当前时刻 = 本机现在。</summary>
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
