namespace Framework.Alarm.Domain
{
    /// <summary>
    /// 事件历史库存储接口（摘要 §8.11，哲学 §8.1【强制】）：append-only，只进不改——
    /// 改历史等于毁证据，KPI 统计全靠它是真的。铁律（摘要 §11 警示 13）：接口上就不存在
    /// 更新、删除路径，Append 是唯一入口；引擎产出的每条事件全量落库，包括带搁置期限的事件。
    /// 保留期 90 天（哲学 §8.1/§12 #3）是运维策略，过期清理不进本接口——届时用独立通道实现，
    /// 本接口的形状永远不含删除。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>追加：只进不改，一条事件一行，永不覆盖、永不删除。</summary>
        void Append(AlarmEvent e);

        /// <summary>查询：按事件时刻窗口 [from, to]（闭区间）、可选位号与可选种类过滤（#18）。
        /// kinds 传 CLR 事件类型（调用方不认识库内的判别名，判别名不暴露在存储接口上）；null 或空 = 全部种类。
        /// 结果按入库先后返回——append-only 库的写入次序就是事件发生次序，过滤只筛不重排。</summary>
        IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds);
    }
}
