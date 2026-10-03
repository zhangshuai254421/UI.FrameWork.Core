using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 搁置契约测试（摘要 §8.10，报警哲学 §7【强制】）：搁置 = 事件打标（<c>ShelvedUntil</c>），不拦状态机——
    /// 搁置期间条件照常触发、状态照常流转、事件照常落库（引擎不撒谎）；UI 按标过滤主列表，
    /// 搁置清单 = <c>Active.Where(ShelvedUntil &gt; now)</c> 单独可见；到期不用定时器解除——时钟一过，标自然失效。
    /// 测试从接缝进：<see cref="AlarmEngine.Shelve"/>/<see cref="AlarmEngine.Unshelve"/> 模拟操作员动作
    /// （时刻取时钟接缝当下，操作员全量留痕）、FakeClock 拨表验证到期、<see cref="AlarmFilters"/> 验证两清单过滤。
    /// 示例档案复用 AlarmDefinitionTests.Sample（限值 250 / 死区 2 / 触发延时 2s / 恢复延时 0 / 高优先级）。
    /// </summary>
    public class AlarmShelveTests
    {
        /// <summary>第二条档案：泵停运报警（限值 1 / 死区 0.1 / 触发延时 1s / 恢复延时 0 / 中优先级）。</summary>
        internal static AlarmDefinition Pump() => AlarmDefinitionTests.Sample() with
        {
            Tag = "PUMP01_STOP",
            Description = "进料泵停运",
            Cause = "电机跳闸或泵体故障",
            OperatorAction = "切换备用泵，通知维修",
            MaxResponseTime = TimeSpan.FromMinutes(30),
            Priority = AlarmPriority.Medium,
            Limit = 1,
            Deadband = 0.1,
            OnDelay = TimeSpan.FromSeconds(1),
            Group = "泵区",
        };

        /// <summary>
        /// 反面约束钉死（摘要 §11 警示 6）：没有「搁置开关」拦状态机——搁置中条件越限，Raised 照产、
        /// 状态照常流转、确认/恢复照常走，历史库全量记录；唯一的差别是 <see cref="AlarmRaised"/>
        /// 和活跃列表快照带着 <c>ShelvedUntil</c> 标（摘要 §8.10：引擎不撒谎，UI 按标过滤）。
        /// 搁置在报警未起时主动下（吹扫场景：先把会乱报的搁上）。
        /// </summary>
        [Fact]
        public void 搁置中触发_状态机照常_历史全量()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            clock.Advance(TimeSpan.FromSeconds(1));                       // T(1)：操作员下单
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(31)

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 265)); // 越限段起点
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 266)); // 满延时升级（T(12) < 期限 T(31)：搁置中）

            var shelved = Assert.IsType<AlarmShelved>(events[0]);         // 搁置动作本身全量留痕
            Assert.Equal(AlarmEngineTests.HEAT01, shelved.Tag);
            Assert.Equal(HysteresisEvaluatorTests.T(1), shelved.At);      // 操作员动作：时刻取时钟当下
            Assert.Equal(HysteresisEvaluatorTests.T(31), shelved.ShelvedUntil); // 期限留痕
            Assert.Equal("李四", shelved.Operator);

            var raised = Assert.IsType<AlarmRaised>(events[1]);           // 状态机照常：Raised 照产，不拦
            Assert.Equal(HysteresisEvaluatorTests.T(12), raised.At);
            Assert.Equal(AlarmPriority.High, raised.Priority);
            Assert.Equal(HysteresisEvaluatorTests.T(31), raised.ShelvedUntil); // 打标：升级发生在搁置中

            var member = Assert.Single(engine.Active);                    // 引擎不撒谎：活跃列表照常收行
            Assert.Equal(AlarmState.Unacknowledged, member.State);
            Assert.Equal(HysteresisEvaluatorTests.T(31), member.ShelvedUntil); // 快照带标（主列表该滤掉它，搁置清单该见到它）

            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "王五");       // 搁置中照常确认（稳红）
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12.5), Value: 245)); // 照常恢复闭环

            Assert.Equal(
                new[] { nameof(AlarmShelved), nameof(AlarmRaised), nameof(AlarmAcknowledged), nameof(AlarmClosed) },
                events.Select(e => e.GetType().Name));                    // 全量记录：一拍不缺，KPI 体检不是瞎子
        }

        /// <summary>
        /// 到期自动恢复播报（验收「到期后（拨表）自动回到正常播报，无任何解除调用」）：
        /// 时钟过了期限——活跃行还在（报警没消失）、标没了（回主列表）；不产 <see cref="AlarmUnshelved"/>
        /// （到期不是动作，无事件）；之后再触发的 <see cref="AlarmRaised"/> 也不带标。
        /// 到期不用定时器解除——时钟一过，标自然失效（摘要 §8.10）。
        /// </summary>
        [Fact]
        public void 搁置_到期拨表_标自然失效_无解除调用()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // T(0) 下单，期限 T(30)

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 266)); // 搁置中升级（带标）
            Assert.Equal(HysteresisEvaluatorTests.T(30), Assert.Single(engine.Active).ShelvedUntil);

            clock.Advance(TimeSpan.FromSeconds(31));                      // T(31)：拨表过期限

            var member = Assert.Single(engine.Active);                    // 报警还挂着：没有谁去「解除」它
            Assert.Null(member.ShelvedUntil);                             // 标自然失效：回主列表正常播报
            Assert.Equal(2, events.Count);                                // 无 Unshelved：到期不产事件

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(13), Value: 245)); // 深回 → RTN
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(40), Value: 270)); // 期限后再越限
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(42), Value: 271)); // 满延时再激活

            var reRaised = Assert.IsType<AlarmRaised>(events[3]);         // RTN 再激活的新一段（Shelved、Raised、AutoReturned 之后）
            Assert.Null(reRaised.ShelvedUntil);                           // 标已过期：新升级不带标
            Assert.Null(Assert.Single(engine.Active).ShelvedUntil);
        }

        /// <summary>
        /// 报警已活跃再搁置：标记落在引擎的搁置登记上，活跃列表快照在读取时对表打标——
        /// 下一次读取即带标（无须等新事件）；状态、优先级、活跃起点一概不动（搁置不拦状态机）。
        /// </summary>
        [Fact]
        public void 搁置_报警已活跃_快照即带标()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // 先升级（未搁置，不带标）
            Assert.Null(Assert.Single(engine.Active).ShelvedUntil);

            clock.Advance(TimeSpan.FromSeconds(5));                       // T(5)：操作员下单
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(35)

            var member = Assert.Single(engine.Active);                    // 下一次快照即带标
            Assert.Equal(HysteresisEvaluatorTests.T(35), member.ShelvedUntil);
            Assert.Equal(AlarmState.Unacknowledged, member.State);        // 状态一概不动
            Assert.Equal(HysteresisEvaluatorTests.T(2), member.Since);
            Assert.Equal(2, events.Count);                                // 只有 Shelve 留痕，无状态事件
            Assert.IsType<AlarmShelved>(events[1]);
        }

        /// <summary>
        /// 解除搁置（验收「每行带解除搁置按钮」的引擎侧）：撤标——快照立刻回主列表；
        /// 产 <see cref="AlarmUnshelved"/> 留痕（操作员动作，时刻取时钟当下）。
        /// </summary>
        [Fact]
        public void 解除_撤标回主列表_产Unshelved留痕()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // Raised
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(30)
            Assert.Equal(HysteresisEvaluatorTests.T(30), Assert.Single(engine.Active).ShelvedUntil); // 搁置清单可见

            clock.Advance(TimeSpan.FromSeconds(2));                       // T(2)：吹扫完了
            engine.Unshelve(AlarmEngineTests.HEAT01, op: "王五");

            var unshelved = Assert.IsType<AlarmUnshelved>(events[2]);     // 动作全量留痕
            Assert.Equal(AlarmEngineTests.HEAT01, unshelved.Tag);
            Assert.Equal(HysteresisEvaluatorTests.T(2), unshelved.At);    // 时钟当下
            Assert.Equal("王五", unshelved.Operator);

            Assert.Null(Assert.Single(engine.Active).ShelvedUntil);       // 撤标：回主列表（报警本身还在）
            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmShelved), nameof(AlarmUnshelved) },
                events.Select(e => e.GetType().Name));
        }

        /// <summary>
        /// 重复搁置以最新下单为准（换班的把期限改短/改长都行），两次下单都各留一行痕
        /// （全量留痕【强制】：每个动作都是本子上一行）。
        /// </summary>
        [Fact]
        public void 搁置_重复搁置_以新期限为准_两次都留痕()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            SeedAndTrigger(engine, source, at: 0);                        // 先有挂着的报警，快照才有标可看

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(30)
            clock.Advance(TimeSpan.FromSeconds(5));                       // T(5)
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(10), op: "王五"); // 期限 T(15)：以新单为准

            var member = Assert.Single(engine.Active);
            Assert.Equal(HysteresisEvaluatorTests.T(15), member.ShelvedUntil);
            Assert.Equal(3, events.Count);                                // Raised + 两次动作两行痕
            Assert.Equal(HysteresisEvaluatorTests.T(30), Assert.IsType<AlarmShelved>(events[1]).ShelvedUntil);
            Assert.Equal(HysteresisEvaluatorTests.T(15), Assert.IsType<AlarmShelved>(events[2]).ShelvedUntil);
            Assert.Equal("王五", Assert.IsType<AlarmShelved>(events[2]).Operator);
        }

        /// <summary>
        /// 搁置必须限时（哲学 §7【强制】，摘要 §11 警示 15）：零/负时长 = 无限期搁置的变体
        /// = 变相删报警——静默拒绝：不登记、不留痕（无效的下单不是动作）。
        /// </summary>
        [Fact]
        public void 搁置_零负时长_静默拒绝()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.Zero, op: "李四");
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(-1), op: "李四");

            Assert.Empty(events);      // 无留痕：没登记成功
            Assert.Null(SeedAndTrigger(engine, source, at: 10).ShelvedUntil); // 升级不带标：标没立上
        }

        /// <summary>无档案的位号搁不上去（与「无档案不报警」同门规）：静默，无留痕、无标。</summary>
        [Fact]
        public void 搁置_无档案位号_静默()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            engine.Shelve("NO_MADB_ENTRY", TimeSpan.FromSeconds(30), op: "李四");

            Assert.Empty(events);
            Assert.Null(SeedAndTrigger(engine, source, at: 10).ShelvedUntil); // 搁置登记上什么都没有：HEAT01 升级照常但不带标
        }

        /// <summary>解除一个没搁置过的位号：静默——没有动作就没有痕，本子上不留 Unshelved 假事件。</summary>
        [Fact]
        public void 解除_未搁置位号_静默()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var events = new List<AlarmEvent>();
            engine.EventRaised += events.Add;

            engine.Unshelve(AlarmEngineTests.HEAT01, op: "李四");   // 从未搁置
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四");
            engine.Unshelve(AlarmEngineTests.HEAT01, op: "王五");   // 正常解除
            engine.Unshelve(AlarmEngineTests.HEAT01, op: "王五");   // 重复解除：静默

            Assert.Equal(
                new[] { nameof(AlarmShelved), nameof(AlarmUnshelved) },
                events.Select(e => e.GetType().Name));              // 只有一对：重复解除不留假痕
        }

        /// <summary>
        /// 搁置清单与主列表过滤各有测试（验收）：两条报警各挂一条，搁置其一——
        /// <see cref="AlarmFilters.Shelved"/> 只见带标的、<see cref="AlarmFilters.MainList"/> 只见不带标的；
        /// 拨表过期限后归位：主列表两条、搁置清单空（过滤规则只有一个：标的期限对不对得上现在）。
        /// </summary>
        [Fact]
        public void 过滤_主列表与搁置清单_各见其份()
        {
            var store = AlarmEngineTests.StoreWithSample();
            store.Add(Pump());
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, store, clock);

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));  // HEAT01 升级
            source.Feed("PUMP01_STOP", new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 5));
            source.Feed("PUMP01_STOP", new ConditionSample(At: HysteresisEvaluatorTests.T(1), Value: 5));              // PUMP01 升级（满 1s 延时）
            Assert.Equal(2, engine.Active.Count);

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 搁置 HEAT01（T(0) 下单，期限 T(30)）
            var now = clock.Now;

            var main = AlarmFilters.MainList(engine.Active, now);
            var shelvedList = AlarmFilters.Shelved(engine.Active, now);
            var mainMember = Assert.Single(main);
            Assert.Equal("PUMP01_STOP", mainMember.Tag);                  // 主列表：只剩没带标的
            var shelvedMember = Assert.Single(shelvedList);
            Assert.Equal(AlarmEngineTests.HEAT01, shelvedMember.Tag);     // 搁置清单：单独可见
            Assert.Equal(HysteresisEvaluatorTests.T(30), shelvedMember.ShelvedUntil);

            clock.Advance(TimeSpan.FromSeconds(29));                      // T(29)：期限前一拍
            Assert.Single(AlarmFilters.Shelved(engine.Active, clock.Now)); // 还没到期：仍在搁置清单

            clock.Advance(TimeSpan.FromSeconds(1));                       // T(30)：时钟一过（期限此刻起不再大于现在）
            Assert.Empty(AlarmFilters.Shelved(engine.Active, clock.Now)); // 搁置清单空
            Assert.Equal(2, AlarmFilters.MainList(engine.Active, clock.Now).Count); // 全员回主列表
        }

        /// <summary>喂满触发延时把 HEAT01 升级（辅助：Shelve 契约测试只要一条挂着的报警）。</summary>
        private static ActiveAlarm SeedAndTrigger(AlarmEngine engine, FakeConditionSource source, double at)
        {
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(at), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(at + 2), Value: 266)); // 满延时升级
            return Assert.Single(engine.Active);
        }
    }
}
