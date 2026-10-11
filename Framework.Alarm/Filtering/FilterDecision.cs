namespace Framework.Alarm
{
    /// <summary>
    /// 滤波决策：条件事实经死区和延时滤波后，够格升级成什么。
    /// 引擎（#12）据此翻译成报警事件，滤波器只判定、不产事件。
    /// </summary>
    public enum FilterDecision
    {
        /// <summary>静默：不升级（限内、延时未满、死区带内等）。</summary>
        None,

        /// <summary>够格报警：越限持续满触发延时。</summary>
        Raise,

        /// <summary>够格清除：回到恢复点（限值 − 死区）以下持续满恢复延时（恢复延时为零则立即清除）。</summary>
        Clear,
    }
}
