namespace Framework.Alarm
{
    /// <summary>
    /// 最大响应时间分档（MADB「优先级+依据」栏的矩阵 T 轴坐标）——优先级矩阵的**输入**。
    /// 默认分档是占位示例（哲学 §4.2：T1 ≤5 分钟 / T2 5–30 分钟 / T3 &gt;30 分钟），宿主重定义。
    /// 矩阵坐标 = (<see cref="AlarmSeverity"/> × <see cref="ResponseTimeTier"/>)，坐标记入档案。
    /// </summary>
    public enum ResponseTimeTier
    {
        /// <summary>T1：需立即处理（默认 ≤5 分钟）。</summary>
        T1,

        /// <summary>T2：需尽快处理（默认 5–30 分钟）。</summary>
        T2,

        /// <summary>T3：可计划处理（默认 &gt;30 分钟）。</summary>
        T3,
    }
}
