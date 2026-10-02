namespace Framework.Alarm
{
    /// <summary>
    /// 四状态五转换报警状态机（报警哲学 §3.1【强制】）。
    /// 每条报警一个实例；判定权住引擎层，本机只管「被人处理到哪一步」。
    /// 非法转换契约（ADR-0009 规则 9，随状态机测试落定）：静默忽略——
    /// 采集层每秒报值、操作员命令有竞态，非法调用不改状态即返回。
    /// </summary>
    public sealed class AlarmStateMachine
    {
        /// <summary>当前状态，初始 Normal。</summary>
        public AlarmState CurrentState { get; private set; } = AlarmState.Normal;

        /// <summary>条件越限触发：Normal 进未确认；非 Normal 时无效（报警中不重播）。</summary>
        public void Trigger()
        {
            if (CurrentState == AlarmState.Normal)
            {
                CurrentState = AlarmState.Unacknowledged;
            }
        }

        /// <summary>操作员确认：未确认进已确认；其余状态无效。</summary>
        public void Acknowledge()
        {
            if (CurrentState == AlarmState.Unacknowledged)
            {
                CurrentState = AlarmState.Acknowledged;
            }
        }

        /// <summary>条件恢复（采集层报事实）：未确认进 RTN、已确认直接回 Normal；其余状态无效。</summary>
        public void Clear()
        {
            if (CurrentState == AlarmState.Unacknowledged)
            {
                CurrentState = AlarmState.ReturnToNormal;
            }
            else if (CurrentState == AlarmState.Acknowledged)
            {
                CurrentState = AlarmState.Normal;
            }
        }

        /// <summary>操作员确认恢复：RTN 回 Normal 闭环；其余状态无效。</summary>
        public void AcknowledgeReturn()
        {
            if (CurrentState == AlarmState.ReturnToNormal)
            {
                CurrentState = AlarmState.Normal;
            }
        }
    }
}
