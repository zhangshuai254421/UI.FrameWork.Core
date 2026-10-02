namespace Framework.Alarm
{
    /// <summary>
    /// 采集层 → 引擎的条件事实接缝（摘要 §8.4）：引擎不直连设备——宿主把设备通讯适配成
    /// 条件事实从本接口报进，测试里 fake 一个直接喂条件序列。采集层只报事实，
    /// 不懂得「报警」二字；越没越限由引擎对照档案限值判定（判定权只住引擎层，ADR-0009 规则 2）。
    /// </summary>
    public interface IConditionSource
    {
        /// <summary>条件事实：某位号某刻的工艺量（CONTEXT.md「条件」——条件 ≠ 状态，别塞进一个 bool）。</summary>
        event Action<string, ConditionSample>? ConditionChanged;
    }
}
