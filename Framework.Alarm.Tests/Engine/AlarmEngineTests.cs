using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// AlarmEngine 总装契约测试（摘要 §8.5–8.9，报警哲学 §3.4/§3.5【强制】）。
    /// 测试从接口进：FakeConditionSource 喂条件序列（引擎不直连设备）、订阅 EventRaised 收事件流
    /// （落库/推 UI 的挂钩）、查 Active 快照、调 Acknowledge 模拟操作员；操作员动作的时间用 FakeClock 控制，
    /// 不真等待、不用 DateTime.Now（ADR-0009 规则 6）。示例档案复用 AlarmDefinitionTests.Sample
    /// （HEAT01_TEMP_HI：限值 250 / 死区 2 / 触发延时 2s / 恢复延时 0 / 高优先级）。
    /// </summary>
    public class AlarmEngineTests
    {
        /// <summary>示例档案的位号（AlarmDefinitionTests.Sample 的 HEAT01_TEMP_HI）。</summary>
        internal const string HEAT01 = "HEAT01_TEMP_HI";

        /// <summary>装着示例档案的内存库。</summary>
        internal static InMemoryAlarmDefinitionStore StoreWithSample()
        {
            var store = new InMemoryAlarmDefinitionStore();
            store.Add(AlarmDefinitionTests.Sample());
            return store;
        }

        /// <summary>
        /// 沉默规则一「无档案不报警」（哲学 §3.4【强制】，摘要 §11 警示 1）：采集层报来的位号
        /// 在 MADB 里没有档案，直接忽略。先立档案再报警，倒逼每条报警都有评审档案。
        /// </summary>
        [Fact]
        public void 进事实_无档案_静默忽略()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, new InMemoryAlarmDefinitionStore(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed("NO_MADB_ENTRY", new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 999)); // 越限再离谱，没有档案也不报

            Assert.Empty(events);   // 不产事件
            Assert.Empty(engine.Active); // 列表也没有
        }

        /// <summary>
        /// 数据流走查 1–4 步（摘要 §8.6）+ 活跃列表四步之一（§8.9）：通讯报值 265℃ → 查档案
        /// （限值 250 / 触发延时 2s）→ 滤波满延时升级 → <see cref="AlarmRaised"/>（闪红开始，进活跃列表）。
        /// 事件带档案优先级和事实时间戳；列表成员 State = Unacknowledged、Since = 升级时刻。
        /// </summary>
        [Fact]
        public void 触发_满延时升级_产Raised进列表()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265)); // 步 1：报值（越限段起点）
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // 步 3：0→2s 满 2s 触发延时

            var raised = Assert.IsType<AlarmRaised>(Assert.Single(events)); // 步 4：Raised 事件发出
            Assert.Equal(HEAT01, raised.Tag);
            Assert.Equal(AlarmPriority.High, raised.Priority);              // 优先级来自档案，不凭主观判断
            Assert.Equal(HysteresisEvaluatorTests.T(2), raised.At);                                  // 条件驱动的事件带事实时间戳

            var member = Assert.Single(engine.Active);                      // 进活跃列表
            Assert.Equal(HEAT01, member.Tag);
            Assert.Equal(AlarmState.Unacknowledged, member.State);          // 四步①：闪红
            Assert.Equal(AlarmPriority.High, member.Priority);
            Assert.Equal(HysteresisEvaluatorTests.T(2), member.Since);                               // 活跃起点 = 升级时刻
        }

        /// <summary>
        /// 沉默规则二「报警中不重复发事件」（哲学 §3.4【强制】，摘要 §11 警示 2）：采集层每秒报值，
        /// Raised 只发一次——升级后继续喂越限事实，不产第二条 Raised，列表也不重复加行。
        /// </summary>
        [Fact]
        public void 触发_报警中继续报值_不重播()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));  // 越限段起点
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));  // 满延时，升级
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(3), Value: 267));  // 采集层每秒照常报值
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(4), Value: 268));
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(5), Value: 269));

            Assert.Single(events);                                          // 只有一条 Raised
            Assert.IsType<AlarmRaised>(events[0]);
            Assert.Single(engine.Active);                                   // 列表也只有一行
        }

        /// <summary>
        /// 不发事件也是正式输出（摘要 §11 警示 19）：越限但触发延时未满——引擎一条事件都不发、列表一行都没有。
        /// 响应窗口从越限起算，延时先占一截（铁律一），这截窗口里引擎必须保持沉默。
        /// </summary>
        [Fact]
        public void 触发_延时未满_全程寂静()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265)); // 越限段起点
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(1.9), Value: 266)); // 差 0.1 秒满延时

            Assert.Empty(events);
            Assert.Empty(engine.Active);
        }

        /// <summary>
        /// 活跃列表四步之二（摘要 §8.9）+ 转换二（哲学 §3.1）：操作员确认，未确认进已确认，
        /// 产 <see cref="AlarmAcknowledged"/>（责任接管，由闪变稳），仍留在列表、State 变 Acknowledged。
        /// 确认是操作员动作：时刻取时钟当前值，操作员名随事件留痕。
        /// </summary>
        [Fact]
        public void 确认_未确认进已确认_产Acknowledged()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, StoreWithSample(), clock);
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // Raised
            clock.Advance(TimeSpan.FromSeconds(3));                        // 三秒后操作员看见

            engine.Acknowledge(HEAT01, op: "李四");

            var acked = Assert.IsType<AlarmAcknowledged>(events[1]);
            Assert.Equal(HEAT01, acked.Tag);
            Assert.Equal("李四", acked.Operator);
            Assert.Equal(HysteresisEvaluatorTests.T(3), acked.At);                                   // 操作员动作时刻 = 时钟当前值

            var member = Assert.Single(engine.Active);                      // 四步②：仍 1 条
            Assert.Equal(AlarmState.Acknowledged, member.State);            // 闪红变稳红
        }

        /// <summary>
        /// 恢复路径一（摘要 §8.8 要求分开测）+ 活跃列表四步之四：确认**之前**条件自行恢复——
        /// 产 <see cref="AlarmAutoReturned"/>（没人看见的恢复，绝不发 Closed），闪绿留在列表等操作员确认。
        /// 两种恢复事件必须分开：早班问「昨夜发生过什么」靠这条区别回答（哲学 §3.5）。
        /// </summary>
        [Fact]
        public void 恢复_未确认时恢复_产AutoReturned留列表RTN()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));  // Raised
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2.5), Value: 245)); // 回到恢复点以下（恢复延时为 0，立即清除）

            var autoReturned = Assert.IsType<AlarmAutoReturned>(events[1]);  // 第二条：AutoReturned
            Assert.Equal(HEAT01, autoReturned.Tag);
            Assert.Equal(HysteresisEvaluatorTests.T(2.5), autoReturned.At);

            var member = Assert.Single(engine.Active);                       // 四步④：仍 1 条
            Assert.Equal(AlarmState.ReturnToNormal, member.State);           // RTN：闪绿等确认
        }

        /// <summary>
        /// 恢复路径二（摘要 §8.8 要求分开测）+ 活跃列表四步之三：确认**之后**条件恢复——
        /// 已经有人接手的报警直接闭环：产 <see cref="AlarmClosed"/>，绝不发 AutoReturned，
        /// 移出活跃列表（列表 0 条）。
        /// </summary>
        [Fact]
        public void 恢复_已确认后恢复_产Closed列表清空()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));   // Raised
            engine.Acknowledge(HEAT01, op: "李四");                            // Acknowledged（时钟在 HysteresisEvaluatorTests.T(0)，动作时刻 = 时钟当前值）
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2.5), Value: 245)); // 回到恢复点以下：已确认的报警直接闭环

            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmAcknowledged), nameof(AlarmClosed) },
                events.Select(e => e.GetType().Name));                        // 事件序列：绝无 AutoReturned

            var closed = Assert.IsType<AlarmClosed>(events[2]);
            Assert.Equal(HysteresisEvaluatorTests.T(2.5), closed.At);                                  // 条件驱动的闭环带事实时间戳
            Assert.Empty(engine.Active);                                      // 四步③：列表 0 条
        }

        /// <summary>
        /// 转换五（哲学 §3.1）：RTN 等到了操作员确认——产 <see cref="AlarmClosedByOperator"/>
        /// 闭环并移出列表。闭环要留痕：时刻取时钟当前值（操作员动作，与条件驱动的闭环区分），
        /// **操作员留名**（#26）——确认、搁置、解除、确认闭环四类操作员动作全量留痕，
        /// 「这条 RTN 是谁确认闭环的」审计答得上来。
        /// </summary>
        [Fact]
        public void 确认恢复_RTN确认后操作员闭环_留名()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, StoreWithSample(), clock);
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));   // Raised
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2.5), Value: 245)); // AutoReturned（进 RTN）
            clock.Advance(TimeSpan.FromSeconds(10));                          // 十秒后夜班的人看见闪绿

            engine.Acknowledge(HEAT01, op: "王五");                            // 补一次确认

            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmAutoReturned), nameof(AlarmClosedByOperator) },
                events.Select(e => e.GetType().Name));                        // 事件序列：没人看见的恢复 + 操作员闭环，两种事件分开

            var closed = Assert.IsType<AlarmClosedByOperator>(events[2]);
            Assert.Equal(HysteresisEvaluatorTests.T(10), closed.At);                                   // 操作员驱动的闭环取时钟当前值
            Assert.Equal("王五", closed.Operator);                            // 留名（#26）：全量留痕的最后一块补齐
            Assert.Empty(engine.Active);                                      // 闭环：移出列表
        }

        /// <summary>
        /// RTN 再激活（状态机补充转换的引擎侧验证）：恢复未确认的报警条件**又**越限满延时——
        /// 产新一段 <see cref="AlarmRaised"/>（闪绿变回闪红，活跃起点刷新），列表仍是一行不是两行；
        /// 之后再次恢复，又一条 AutoReturned。夜里的每一段反复都留在事件记录里——「历史完整性」
        /// （哲学 §3.5）：早班问「昨夜发生过什么」，靠的就是这份完整。
        /// </summary>
        [Fact]
        public void 恢复_RTN后再越限_再激活产新Raised()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));    // Raised 第一段
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2.5), Value: 245));  // 恢复 → AutoReturned（RTN）

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 275));   // 又越限：延时从零累计
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(11), Value: 276));   // 未满 2s：静默
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 277));   // 满 2s → 再激活

            var member = Assert.Single(engine.Active);                          // 仍是一行，不是两行
            Assert.Equal(AlarmState.Unacknowledged, member.State);              // 闪绿变回闪红
            Assert.Equal(HysteresisEvaluatorTests.T(12), member.Since);         // 活跃起点刷新为新一段

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12.5), Value: 244)); // 再次恢复

            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmAutoReturned),
                        nameof(AlarmRaised), nameof(AlarmAutoReturned) },
                events.Select(e => e.GetType().Name));                          // 两段反复，每段都有记录
            Assert.Equal(AlarmState.ReturnToNormal, Assert.Single(engine.Active).State);
        }

        /// <summary>
        /// 集成剧本：数据流走查六步（摘要 §8.6）一条流跑完，确认、恢复两个分支都走到。
        /// 订阅方（本测试的 events 列表）就是落库/推 UI 的挂钩——订阅即落库，一行接线（摘要 §8.11）。
        /// 步 1 通讯报值 → 步 2 引擎查档案 → 步 3 满延时 Raised → 步 4 事件发出 →
        /// 步 5 确认后降温闭环（Closed）→ 步 6 没人确认自行恢复（AutoReturned，闪绿留列表）。
        /// </summary>
        [Fact]
        public void 集成_数据流走查六步_两分支()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();       // 替身历史库：append-only 收全量
            engine.EventRaised += events.Add;

            // 第一轮：步 1–5（确认分支）
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));    // 步 1：通讯报值（越限）
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));    // 步 2–3：查档案、满延时 → Raised
            Assert.Single(events);                                             // 步 4：事件发出，订阅方收到
            Assert.IsType<AlarmRaised>(events[0]);
            Assert.Single(engine.Active);

            engine.Acknowledge(HEAT01, op: "李四");                            // 步 5 前半：确认（稳红）
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2.5), Value: 245));  // 步 5 后半：降温过恢复点 → Closed
            Assert.Empty(engine.Active);                                       // 全链路闭环，移出列表

            // 第二轮：步 6（恢复分支）——没人确认，温度自己掉了回来
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 270));   // 再越限：新一轮延时从零累计
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 271));   // 满 2s → 又 Raised
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12.5), Value: 240)); // 没人确认就掉回 → AutoReturned

            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmAcknowledged), nameof(AlarmClosed),
                        nameof(AlarmRaised), nameof(AlarmAutoReturned) },
                events.Select(e => e.GetType().Name));                         // 事件序列：两个分支各自成立、先后不乱

            var member = Assert.Single(engine.Active);                         // 步 6 落点：闪绿 RTN 留列表
            Assert.Equal(AlarmState.ReturnToNormal, member.State);
        }

        /// <summary>非法转换契约的引擎侧验证（ADR-0009 规则 9，静默忽略）：Normal 时确认、重复确认、
        /// 无档案位号的确认——一律无事件、列表不动。</summary>
        [Fact]
        public void 确认_非法时序_静默忽略()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            engine.Acknowledge("NO_MADB_ENTRY", op: "李四"); // 无档案：静默
            engine.Acknowledge(HEAT01, op: "李四");          // Normal 时确认：静默

            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // Raised
            engine.Acknowledge(HEAT01, op: "李四");          // 第一次确认：真转换
            engine.Acknowledge(HEAT01, op: "王五");          // 重复确认：静默，不产第二条 Acknowledged

            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmAcknowledged) },
                events.Select(e => e.GetType().Name));       // 事件序列干净：非法时序没留假事件
        }
    }
}
