namespace Framework.Alarm
{
    /// <summary>
    /// 模块时间接缝（ADR-0009 规则 6【强制】）：凡是依赖时间的逻辑都从这里看表——
    /// 注入时钟才能「拨表」测试，禁止直连 <c>DateTime.Now</c>。
    /// 滤波器的延时累计用条件事实自带的时间戳（见 <see cref="HysteresisEvaluator"/>）；
    /// 引擎侧的驻留、搁置到期等判定消费本接缝（#12 引擎总装起）。
    /// </summary>
    public interface IAlarmClock
    {
        /// <summary>当前时刻。</summary>
        DateTimeOffset Now { get; }
    }
}
