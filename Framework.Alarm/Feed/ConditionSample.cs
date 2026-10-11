namespace Framework.Alarm
{
    /// <summary>
    /// 条件事实：采集层报来的某时刻工艺量（CONTEXT.md「条件」。有没有越限由滤波器对照限值判定，
    /// 采集层只报值）。<c>At</c> 是采集时间戳，延时累计一律用它，不用引擎本地时钟（ADR-0009 规则 6）。
    /// </summary>
    /// <param name="At">采集时间戳。</param>
    /// <param name="Value">工艺量瞬时值。</param>
    public sealed record ConditionSample(DateTimeOffset At, double Value);
}
