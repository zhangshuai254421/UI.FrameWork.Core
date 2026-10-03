using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 搁置订单持久化恢复契约（#16）：搁置登记 = 事件流的投影（与活跃列表同一哲学）——
    /// 引擎总装时若接了历史库，按落库先后回放 <see cref="AlarmShelved"/>/<see cref="AlarmUnshelved"/>
    /// 重建登记本：未到期订单照常有效（操作员的 8 小时搁置单不因重启清零）、解除过的不复活、
    /// 已到期的不复活、重复搁置以最新下单为准。append-only 铁律不受触碰：回放是读历史，不是改历史。
    /// </summary>
    public class AlarmEngineShelveRestoreTests
    {
        /// <summary>
        /// 验收主场景：操作员下搁置单 → 主机重启 → 新引擎从历史库回放登记——
        /// 重启后条件再越限，升级事件从重建的登记本带标：报警照旧留在搁置清单被压制，
        /// 8 小时的搁置单不因重启清零。（登记本是回放对象；活跃列表不在本票范围。）
        /// </summary>
        [Fact]
        public void 重启回放_未到期订单_新引擎照常带标()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // T(0) 下单，期限 T(30)
            engine.Dispose();                                             // 重启：旧引擎退订采集层（改档重建同口径）

            clock.Advance(TimeSpan.FromSeconds(10));                      // T(10)：新引擎总装
            var reborn = new AlarmEngine(source, definitions, clock, history);
            var raisedEvents = new List<AlarmEvent>();
            reborn.EventRaised += raisedEvents.Add;

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 265)); // 重启后越限段起点
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(14), Value: 266)); // 满延时升级

            var raised = Assert.IsType<AlarmRaised>(Assert.Single(raisedEvents));
            Assert.Equal(HysteresisEvaluatorTests.T(30), raised.ShelvedUntil); // 升级带标：订单活着（回放重建的登记）
            var member = Assert.Single(reborn.Active);
            Assert.Equal(HysteresisEvaluatorTests.T(30), member.ShelvedUntil); // 搁置清单见它，主列表滤掉它
        }

        /// <summary>解除过的订单不随回放复活：Unshelved 的撤账在回放里同样生效。</summary>
        [Fact]
        public void 重启回放_解除过的订单_不复活()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(30)
            clock.Advance(TimeSpan.FromSeconds(2));                       // T(2)：吹扫完了
            engine.Unshelve(AlarmEngineTests.HEAT01, op: "王五");          // 解除留痕
            engine.Dispose();

            var reborn = new AlarmEngine(source, definitions, clock, history);
            var events = new List<AlarmEvent>();
            reborn.EventRaised += events.Add;

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(5), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(7), Value: 266)); // 新引擎里升级

            var member = Assert.Single(reborn.Active);
            Assert.Null(member.ShelvedUntil);                             // 解除过的单子：撤账回放生效，不带标
        }

        /// <summary>已到期的订单不复活：回放后对表清旧账——时钟一过标自然失效，重启不给死单翻案。</summary>
        [Fact]
        public void 重启回放_已到期订单_不复活()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(30)
            clock.Advance(TimeSpan.FromSeconds(31));                      // T(31)：订单已自然到期
            engine.Dispose();

            var reborn = new AlarmEngine(source, definitions, clock, history);
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(35), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(37), Value: 266));

            var member = Assert.Single(reborn.Active);
            Assert.Null(member.ShelvedUntil);                             // 到期的单子：不复活，升级不带标
        }

        /// <summary>重复搁置以最新下单为准：回放按落库先后走一遍，后单覆盖前单（与运行期同规矩）。</summary>
        [Fact]
        public void 重启回放_重复搁置_以最新下单为准()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(30)
            clock.Advance(TimeSpan.FromSeconds(5));                       // T(5)
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(10), op: "王五"); // 期限 T(15)：新单
            clock.Advance(TimeSpan.FromSeconds(1));                       // T(6)
            engine.Dispose();

            var reborn = new AlarmEngine(source, definitions, clock, history);
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 266));

            var member = Assert.Single(reborn.Active);
            Assert.Equal(HysteresisEvaluatorTests.T(15), member.ShelvedUntil); // 后单为准，不是前单的 T(30)
        }

        /// <summary>append-only 边界（本票的规矩线）：回放是读历史，不是改历史——
        /// 新引擎总装期间不往库里追加一行，也不因回放对外吐一拍事件。</summary>
        [Fact]
        public void 回放_只读不改历史_不吐事件()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四");
            engine.Dispose();
            Assert.Equal(1, history.Query(DateTimeOffset.MinValue, DateTimeOffset.MaxValue).Count);

            var rebornEvents = new List<AlarmEvent>();
            var reborn = new AlarmEngine(source, definitions, clock, history);
            reborn.EventRaised += rebornEvents.Add;

            Assert.Equal(1, history.Query(DateTimeOffset.MinValue, DateTimeOffset.MaxValue).Count); // 一行不多
            Assert.Empty(rebornEvents);                                   // 一拍不吐：回放不是播报
        }
    }
}
