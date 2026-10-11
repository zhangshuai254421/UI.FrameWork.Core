namespace Framework.Alarm
{
    /// <summary>
    /// 四状态转换报警状态机（报警哲学 §3.1【强制】的五个转换，外加下面说明的 RTN 再激活）。
    /// 每条报警一个实例；越限判定在引擎层做，本类只管「操作员处理到哪一步」。
    /// 非法转换一律静默忽略，不改状态直接返回（ADR-0009 规则 9，随状态机测试落定）：
    /// 采集层每秒报值、操作员命令又有竞态，非法调用是常态，不该抛异常。
    /// RTN 再激活（引擎总装评审落定的补充转换）：已恢复未确认的报警条件再次越限 → 回到未确认。
    /// 五转换表没覆盖这种情况，但直接丢掉整段再越限违反「历史完整性」（哲学 §3.5）；
    /// ISA-18.2 状态图里有这个转换，随测试落定，等哲学改版补录。
    /// </summary>
    public sealed class AlarmStateMachine
    {
        /// <summary>当前状态，初始 Normal。</summary>
        public AlarmState CurrentState { get; private set; } = AlarmState.Normal;

        /// <summary>
        /// 条件越限触发：Normal 进未确认（唯一入口）；RTN 再激活——已恢复未确认的报警条件又越限了，
        /// 回到未确认。报警还没人确认，条件就还在不正常，它仍然是一条正在发生的报警（ISA-18.2 口径）。
        /// 未确认、已确认状态下无效：报警持续期间不重复发事件，Raised 只发一次。
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
