using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 搁置登记持久化恢复契约（#16）：搁置登记表 = 事件流的投影（与活跃列表同一哲学）——
    /// 引擎构造时若接了历史库，按入库先后回放 <see cref="AlarmShelved"/>/<see cref="AlarmUnshelved"/>
    /// 重建登记表：未到期的搁置照常有效（操作员的 8 小时搁置单不因重启清零）、解除过的不再生效、
    /// 已到期的不再生效、重复搁置以最后一次为准。append-only 铁律不受影响：回放是读历史，不是改历史；
    /// 回放只筛搁置和解除两种类型让库去筛（#18），不把全时间窗全类型的记录都读进内存。
    /// </summary>
    public class AlarmEngineShelveRestoreTests
    {
        /// <summary>
        /// 验收主场景：操作员下搁置单 → 主机重启 → 新引擎从历史库回放登记表——
        /// 重启后条件再越限，升级事件从重建的登记表带上期限：报警照旧留在搁置清单被压制，
        /// 8 小时的搁置单不因重启清零。（回放的对象是登记表；活跃列表不在本票范围。）
        /// </summary>
        [Fact]
        public void 重启回放_未到期搁置单_新引擎照常带期限()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // T(0) 登记，期限 T(30)
            engine.Dispose();                                             // 重启：旧引擎退订采集层（换档案重建同理）

            clock.Advance(TimeSpan.FromSeconds(10));                      // T(10)：新引擎组装
            var reborn = new AlarmEngine(source, definitions, clock, history);
            var raisedEvents = new List<AlarmEvent>();
            reborn.EventRaised += raisedEvents.Add;

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 265)); // 重启后越限段起点
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(14), Value: 266)); // 满延时升级

            var raised = Assert.IsType<AlarmRaised>(Assert.Single(raisedEvents));
            Assert.Equal(HysteresisEvaluatorTests.T(30), raised.ShelvedUntil); // 升级带期限：搁置还活着（回放重建的登记）
            var member = Assert.Single(reborn.Active);
            Assert.Equal(HysteresisEvaluatorTests.T(30), member.ShelvedUntil); // 搁置清单见它，主列表滤掉它
        }

        /// <summary>解除过的搁置回放后不再生效：Unshelved 的取消在回放里同样生效。</summary>
        [Fact]
        public void 重启回放_解除过的搁置单_不再生效()
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
            Assert.Null(member.ShelvedUntil);                             // 解除过的搁置：取消在回放中生效，不带期限
        }

        /// <summary>已到期的搁置不再生效：回放后用当前时刻清理——期限一过自然失效，重启不改过期结论。</summary>
        [Fact]
        public void 重启回放_已到期搁置单_不再生效()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(30)
            clock.Advance(TimeSpan.FromSeconds(31));                      // T(31)：搁置已自然到期
            engine.Dispose();

            var reborn = new AlarmEngine(source, definitions, clock, history);
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(35), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(37), Value: 266));

            var member = Assert.Single(reborn.Active);
            Assert.Null(member.ShelvedUntil);                             // 到期的搁置：不再生效，升级不带期限
        }

        /// <summary>重复搁置以最后一次为准：回放按入库先后走一遍，后一次覆盖前一次（与运行期同一规矩）。</summary>
        [Fact]
        public void 重启回放_重复搁置_以最新登记为准()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var definitions = AlarmEngineTests.StoreWithSample();
            var history = new InMemoryEventStore();
            var engine = new AlarmEngine(source, definitions, clock, history);
            engine.EventRaised += history.Append;

            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(30), op: "李四"); // 期限 T(30)
            clock.Advance(TimeSpan.FromSeconds(5));                       // T(5)
            engine.Shelve(AlarmEngineTests.HEAT01, TimeSpan.FromSeconds(10), op: "王五"); // 期限 T(15)：最后一次
            clock.Advance(TimeSpan.FromSeconds(1));                       // T(6)
            engine.Dispose();

            var reborn = new AlarmEngine(source, definitions, clock, history);
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 266));

            var member = Assert.Single(reborn.Active);
            Assert.Equal(HysteresisEvaluatorTests.T(15), member.ShelvedUntil); // 最后一次为准，不是前一次的 T(30)
        }

        /// <summary>append-only 边界（本票的规矩线）：回放是读历史，不是改历史——
        /// 新引擎组装期间不往库里追加一行，也不因回放对外发一条事件。</summary>
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
            Assert.Empty(rebornEvents);                                   // 一条不发：回放不是播报
        }

        /// <summary>回放只筛搁置两种类型（#18）：引擎组装时在库里按种类筛——把全时间窗全类型
        /// 记录都读进内存是库一大就卡死的根源，引擎只拉搁置/解除两种；筛选按 CLR 事件类型，
        /// 库内的判别名不暴露在存储接口上。</summary>
        [Fact]
        public void 回放_按类型筛搁置两种_不全窗全类物化()
        {
            var probing = new ProbingEventStore();

            _ = new AlarmEngine(
                new FakeConditionSource(), AlarmEngineTests.StoreWithSample(),
                new FakeClock(HysteresisEvaluatorTests.T(0)), probing);

            var query = Assert.Single(probing.Queries);
            Assert.Equal(
                new[] { typeof(AlarmShelved), typeof(AlarmUnshelved) },
                query);                                                   // 筛选清单：只有搁置与解除
        }

        /// <summary>探针替身：只为记录引擎回放时请求筛选的种类，别无他用。</summary>
        private sealed class ProbingEventStore : IEventStore
        {
            /// <summary>每次 Query 收到的种类清单，一次调用记一行。</summary>
            public List<Type[]> Queries { get; } = [];

            public void Append(AlarmEvent e)
            {
            }

            public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
            {
                Queries.Add(kinds);
                return [];
            }
        }
    }
}
