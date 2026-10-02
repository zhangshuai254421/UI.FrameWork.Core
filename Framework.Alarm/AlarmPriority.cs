namespace Framework.Alarm
{
    /// <summary>
    /// 报警优先级，固定三级（哲学 §4.1【强制】，§12 #1 拍板）——优先级矩阵的**输出**：
    /// 查 (严重度 × 响应时间) 矩阵得出，坐标记入档案，不凭嗓门。
    /// 宿主要四级须先改框架、走 ADR。
    /// </summary>
    public enum AlarmPriority
    {
        /// <summary>高：立即响应（目标配比 ~5%，哲学 §4.3）。</summary>
        High,

        /// <summary>中：尽快响应（目标配比 ~15%）。</summary>
        Medium,

        /// <summary>低：可计划响应（目标配比 ~80%）。</summary>
        Low,
    }
}
