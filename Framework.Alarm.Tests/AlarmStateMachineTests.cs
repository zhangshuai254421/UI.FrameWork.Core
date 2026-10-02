using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 四状态五转换状态机契约测试（报警哲学 §3.1【强制】）。
    /// 非法转换契约（ADR-0009 规则 9）随本套测试落定：<c>静默忽略</c>——
    /// 采集层每秒报值、操作员命令有竞态，非法调用不改状态即返回；
    /// Normal 时触发是唯一入口（「报警中不重播」的根基）。
    /// </summary>
    public class AlarmStateMachineTests
    {
        /// <summary>转换一：Normal 时条件越限触发，进入未确认。</summary>
        [Fact]
        public void 触发_Normal进未确认()
        {
            var machine = new AlarmStateMachine();

            machine.Trigger();

            Assert.Equal(AlarmState.Unacknowledged, machine.CurrentState);
        }

        /// <summary>转换二：操作员确认，未确认进已确认（由闪变稳，责任接管）。</summary>
        [Fact]
        public void 确认_未确认进已确认()
        {
            var machine = new AlarmStateMachine();
            machine.Trigger();

            machine.Acknowledge();

            Assert.Equal(AlarmState.Acknowledged, machine.CurrentState);
        }

        /// <summary>转换三：确认之前条件自愈，未确认进 RTN（闪绿留列表等「看见了」）。</summary>
        [Fact]
        public void 条件恢复_未确认进RTN()
        {
            var machine = new AlarmStateMachine();
            machine.Trigger();

            machine.Clear();

            Assert.Equal(AlarmState.ReturnToNormal, machine.CurrentState);
        }

        /// <summary>
        /// 转换四 + RTN 口径保真项（哲学 §3.1、摘要 §4.3【标准】）：
        /// RTN 只从「未确认」进入——已确认的报警条件恢复直接回 Normal 移出列表，
        /// 「有主人的报警，回家不用再敲门」。与转换三是两条路径，测试分开写。
        /// </summary>
        [Fact]
        public void 条件恢复_已确认直接回Normal_不进RTN()
        {
            var machine = new AlarmStateMachine();
            machine.Trigger();
            machine.Acknowledge();

            machine.Clear();

            Assert.Equal(AlarmState.Normal, machine.CurrentState);
        }

        /// <summary>转换五：操作员确认恢复，RTN 回 Normal 移出列表，闭环。</summary>
        [Fact]
        public void 确认恢复_RTN确认后回Normal闭环()
        {
            var machine = new AlarmStateMachine();
            machine.Trigger();
            machine.Clear();
            Assert.Equal(AlarmState.ReturnToNormal, machine.CurrentState); // 前置：确实在 RTN

            machine.AcknowledgeReturn();

            Assert.Equal(AlarmState.Normal, machine.CurrentState);
        }

        /// <summary>
        /// 沉默决策「报警中不重播」（哲学 §3.4【强制】）：采集层每秒报值，
        /// 未确认/已确认时触发无效——条件持续越限期间 Raised 只发一次。
        /// （RTN 不在此列：RTN 的含义是条件已回正常，再来触发就是新越限，见 RTN 再激活测试。）
        /// </summary>
        [Fact]
        public void 触发_报警中不重播_状态不变()
        {
            var unack = new AlarmStateMachine();
            unack.Trigger();
            unack.Trigger();
            Assert.Equal(AlarmState.Unacknowledged, unack.CurrentState);

            var acked = new AlarmStateMachine();
            acked.Trigger();
            acked.Acknowledge();
            acked.Trigger();
            Assert.Equal(AlarmState.Acknowledged, acked.CurrentState);
        }

        /// <summary>
        /// RTN 再激活（引擎总装评审落定的补充转换，ISA-18.2 口径）：恢复未确认的报警条件**又**越限，
        /// 回未确认闪红——报警还没人看见，条件就还不正常，它就是一条正在发生的报警。
        /// 静默丢掉这段再越限会违反「历史完整性」（哲学 §3.5）：早班的「昨夜发生过什么」
        /// 恰恰是这些夜里反复的段落。哲学 v1.0 的五转换表未覆盖此格，随本测试落定，待哲学改版补录。
        /// </summary>
        [Fact]
        public void 触发_RTN再激活_回未确认()
        {
            var machine = new AlarmStateMachine();
            machine.Trigger();
            machine.Clear();
            Assert.Equal(AlarmState.ReturnToNormal, machine.CurrentState); // 前置：确实在 RTN

            machine.Trigger(); // 条件再越限（满触发延时的新事实，不是每秒重播）

            Assert.Equal(AlarmState.Unacknowledged, machine.CurrentState); // 闪绿变回闪红
        }

        /// <summary>非法转换契约（ADR-0009 规则 9，落定：静默忽略）：非未确认状态时确认无效。</summary>
        [Fact]
        public void 确认_非未确认状态_状态不变()
        {
            var normal = new AlarmStateMachine();
            normal.Acknowledge();
            Assert.Equal(AlarmState.Normal, normal.CurrentState);

            var acked = new AlarmStateMachine();
            acked.Trigger();
            acked.Acknowledge();
            acked.Acknowledge(); // 重复确认
            Assert.Equal(AlarmState.Acknowledged, acked.CurrentState);

            var rtn = new AlarmStateMachine();
            rtn.Trigger();
            rtn.Clear();
            rtn.Acknowledge(); // RTN 的出路是确认恢复，不是确认
            Assert.Equal(AlarmState.ReturnToNormal, rtn.CurrentState);
        }

        /// <summary>非法转换契约（ADR-0009 规则 9，落定：静默忽略）：Normal 时没有可恢复的报警；RTN 已恢复，等的是确认不是再恢复。</summary>
        [Fact]
        public void 条件恢复_Normal或RTN时_状态不变()
        {
            var normal = new AlarmStateMachine();
            normal.Clear();
            Assert.Equal(AlarmState.Normal, normal.CurrentState);

            var rtn = new AlarmStateMachine();
            rtn.Trigger();
            rtn.Clear();
            rtn.Clear(); // 重复恢复
            Assert.Equal(AlarmState.ReturnToNormal, rtn.CurrentState);
        }

        /// <summary>非法转换契约（ADR-0009 规则 9，落定：静默忽略）：确认恢复只在 RTN 有意义。</summary>
        [Fact]
        public void 确认恢复_非RTN状态_状态不变()
        {
            var normal = new AlarmStateMachine();
            normal.AcknowledgeReturn();
            Assert.Equal(AlarmState.Normal, normal.CurrentState);

            var unack = new AlarmStateMachine();
            unack.Trigger();
            unack.AcknowledgeReturn(); // 未确认的出路是确认，不是确认恢复
            Assert.Equal(AlarmState.Unacknowledged, unack.CurrentState);

            var acked = new AlarmStateMachine();
            acked.Trigger();
            acked.Acknowledge();
            acked.AcknowledgeReturn(); // 已确认恢复走 Clear 直接回 Normal，不走确认恢复
            Assert.Equal(AlarmState.Acknowledged, acked.CurrentState);
        }
    }
}
