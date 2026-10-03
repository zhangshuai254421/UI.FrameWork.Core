namespace Framework.Alarm.Tests
{
    /// <summary>
    /// EventHistory 接线契约（摘要 §8.11、哲学 §8.1【强制】）：订阅即落库——构造即把
    /// 引擎事件流接到存储 Append，引擎产出的每一拍自动进本子；引擎静默的每一拍库里也
    /// 无假事件（寂静也是断言）。联测走真实引擎：FakeConditionSource 喂条件、InMemoryEventStore
    /// 收流，复用 #12 引擎总装的数据流走查六拍剧本。
    /// </summary>
    public class EventHistoryTests
    {
        /// <summary>订阅即落库：引擎产一拍，库里多一行——一行接线，不需要谁记得手动落库。</summary>
        [Fact]
        public void 接线_引擎产事件_自动落库()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var store = new InMemoryEventStore();
            _ = new EventHistory(store, engine);

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // 满延时 → Raised

            var raised = Assert.IsType<AlarmRaised>(Assert.Single(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10))));
            Assert.Equal(AlarmPriority.High, raised.Priority);              // 档案优先级随事件进本子
            Assert.Equal(HysteresisEvaluatorTests.T(2), raised.At);
        }

        /// <summary>
        /// 验收联测：数据流走查六拍剧本（摘要 §8.6，与引擎总装票同一剧本）跑完，
        /// 库里恰好五条、先后不乱、内容无损——两分支（确认闭环/无人看见自愈）的每一拍都有记录。
        /// </summary>
        [Fact]
        public void 接线_六拍剧本_每一拍在库()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var store = new InMemoryEventStore();
            _ = new EventHistory(store, engine);

            SixBeatScript.Run(source, engine, clock);

            var history = store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(13));
            Assert.Equal(SixBeatScript.ExpectedEventTypes, history.Select(e => e.GetType().Name)); // 每一拍在库：履历完整，两分支分立

            var first = Assert.IsType<AlarmRaised>(history[0]);
            Assert.Equal(HysteresisEvaluatorTests.T(2), first.At);       // 条件驱动：带事实时间戳
            var closed = Assert.IsType<AlarmClosed>(history[2]);
            Assert.Equal(HysteresisEvaluatorTests.T(2.5), closed.At);
            var last = Assert.IsType<AlarmAutoReturned>(history[4]);
            Assert.Equal(HysteresisEvaluatorTests.T(12.5), last.At);     // 无人看见的恢复：单独留痕，绝没有混成 Closed
        }

        /// <summary>引擎静默，库里也干净（寂静也是断言，§11 警示 19）：无档案位号、Normal 时确认、
        /// 重复确认——本子上一条假事件都没有；KPI 体检不能被非法时序的幽灵事件污染。</summary>
        [Fact]
        public void 接线_静默拍_库里无假事件()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var store = new InMemoryEventStore();
            _ = new EventHistory(store, engine);

            source.Feed("NO_MADB_ENTRY", new ConditionSample(At: HysteresisEvaluatorTests.T(1), Value: 999)); // 无档案：静默
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "李四");          // Normal 时确认：静默

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // Raised
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "李四");          // 第一次确认：真转换
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "王五");          // 重复确认：静默

            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmAcknowledged) },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)).Select(e => e.GetType().Name));
        }
    }
}
