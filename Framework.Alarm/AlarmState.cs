namespace Framework.Alarm
{
    /// <summary>
    /// 报警状态机四态（报警哲学 §3.1【强制】，CONTEXT.md「报警状态」）：
    /// 管理「这条报警被人处理到哪一步」，与「越没越限」的条件是两回事。
    /// 显示惯例（ISA-18.1）：未确认=闪红、已确认=稳红、RTN=闪绿。
    /// </summary>
    public enum AlarmState
    {
        /// <summary>正常：无报警；触发是进报警的唯一入口。</summary>
        Normal,

        /// <summary>未确认：触发后等操作员「看见了」（闪红）。</summary>
        Unacknowledged,

        /// <summary>已确认：责任已接管（稳红）；条件恢复直接回 Normal，不进 RTN。</summary>
        Acknowledged,

        /// <summary>恢复未确认（RTN）：确认之前条件自愈，闪绿留列表等一个「看见了」。</summary>
        ReturnToNormal,
    }
}
