namespace Framework.Alarm
{
    /// <summary>
    /// 四状态转换报警状态机（报警哲学 §3.1【强制】的五转换 + RTN 再激活，见下）。
    /// 每条报警一个实例；判定权住引擎层，本机只管「被人处理到哪一步」。
    /// 非法转换契约（ADR-0009 规则 9，随状态机测试落定）：静默忽略——
    /// 采集层每秒报值、操作员命令有竞态，非法调用不改状态即返回。
    /// RTN 再激活（引擎总装评审落定的补充转换）：恢复未确认的报警条件再越限 → 回未确认——
    /// 五转换表没覆盖这一格，但静默丢掉整段再越限违反「历史完整性」（哲学 §3.5）；
    /// ISA-18.2 状态图有此转换，随测试落定，待哲学改版补录。
    /// </summary>
    public sealed class AlarmStateMachine
    {
        /// <summary>当前状态，初始 Normal。</summary>
        public AlarmState CurrentState { get; private set; } = AlarmState.Normal;

        /// <summary>
        /// 条件越限触发：Normal 进未确认（唯一入口）；RTN 再激活——恢复未确认的报警条件又越限了，
        /// 回未确认闪红（报警还没人看见，条件就还不正常，它就是一条正在发生的报警；ISA-18.2 口径）。
        /// 未确认/已确认时无效（报警中不重播——条件持续越限，Raised 只发一次）。
        /// </summary>
        public void Trigger()
        {
            if (CurrentState == AlarmState.Normal || CurrentState == AlarmState.ReturnToNormal)
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
