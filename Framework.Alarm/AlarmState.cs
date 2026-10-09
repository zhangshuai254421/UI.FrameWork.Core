namespace Framework.Alarm
{
    /// <summary>
    /// 报警状态机四态（报警哲学 §3.1【强制】，CONTEXT.md「报警状态」）：
    /// 表示「这条报警被人处理到哪一步」，和「有没有越限」的条件是两回事。
    /// 显示惯例（ISA-18.1）：未确认 = 闪红、已确认 = 稳红、RTN = 闪绿。
    /// </summary>
    public enum AlarmState
    {
        /// <summary>正常：无报警；触发是进入报警状态的唯一入口。</summary>
        Normal,

        /// <summary>未确认：触发后等操作员确认（闪红）。</summary>
        Unacknowledged,

        /// <summary>已确认：责任已接管（稳红）；条件恢复直接回 Normal，不进 RTN。</summary>
        Acknowledged,

        /// <summary>恢复未确认（RTN）：确认之前条件自行恢复，闪绿留在列表等操作员确认。</summary>
        ReturnToNormal,
    }
}
