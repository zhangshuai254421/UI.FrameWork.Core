namespace Framework.Alarm
{
    /// <summary>
    /// 采集层到引擎的条件事实接口（摘要 §8.4）：引擎不直连设备。宿主把设备通讯适配成条件事实
    /// 从本接口报进来，测试里造一个假的直接喂条件序列。采集层只报事实，不懂「报警」二字；
    /// 有没有越限由引擎对照档案限值判定（判定逻辑只在引擎层，ADR-0009 规则 2）。
    /// </summary>
    public interface IConditionSource
    {
        /// <summary>条件事实：某位号某时刻的工艺量（CONTEXT.md「条件」。条件不等于状态，不要塞进一个 bool）。</summary>
        event Action<string, ConditionSample>? ConditionChanged;
    }
}
