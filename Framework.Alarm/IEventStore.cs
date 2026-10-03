namespace Framework.Alarm
{
    /// <summary>
    /// 事件历史库存储接缝（摘要 §8.11，哲学 §8.1【强制】）：append-only 只进不改——
    /// 改历史 = 毁证据，KPI 体检全靠它真。铁律（摘要 §11 警示 13）：接口上就不存在
    /// 更新/删除路径，Append 是唯一入口；引擎产出的每一拍全量落库，包括带搁置标的
    /// 事件（标本身随搁置工单落地）。保留期 90 天（哲学 §8.1/§12 #3）是运维策略，
    /// 过期清理不进本接口——届时以独立通道实现，本接口的形状永不含删除。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>追加：只进不改，一条事件一行，永不覆盖、永不删除。</summary>
        void Append(AlarmEvent e);

        /// <summary>查询：按事件时刻窗口 [from, to]（闭区间）与可选位号过滤；
        /// 结果按落库先后返回——append-only 本子的书写次序即履历次序，窗口只筛不排。</summary>
        IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null);
    }
}
