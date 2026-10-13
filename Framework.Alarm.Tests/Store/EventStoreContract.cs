using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 事件存储契约：内存替身与 SQLite 实现共用同一套行为断言，两套各测一遍。
    /// 契约方法只断言、不建存储，存储由各测试套件注入。
    /// 依据：摘要 §8.11、哲学 §8.1【强制】、§11 警示 13（append-only）。
    /// </summary>
    internal static class EventStoreContract
    {
        /// <summary>追加后查询可见（验收项）：Raised 存进去，时间窗内原样读回（record 值相等）。</summary>
        public static void 追加_查询可见_原样读回(IEventStore store)
        {
            var raised = new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High);

            store.Append(raised);

            var found = Assert.Single(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)));
            Assert.Equal(raised, found);
        }

        /// <summary>四种事件都留痕（哲学 §3.5）：确认带操作员，AutoReturned 与 Closed 分立，
        /// 早班查「昨夜发生过什么」靠这条区别。四条各自原样读回，值相等、字段不丢。</summary>
        public static void 四种事件_往返保真(IEventStore store)
        {
            var raised = new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High);
            var acknowledged = new AlarmAcknowledged(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(3), "李四");
            var autoReturned = new AlarmAutoReturned("PUMP01_STOP", HysteresisEvaluatorTests.T(4));
            var closed = new AlarmClosed(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(5));

            store.Append(raised);
            store.Append(acknowledged);
            store.Append(autoReturned);
            store.Append(closed);

            var history = store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10));
            Assert.Equal(
                new AlarmEvent[] { raised, acknowledged, autoReturned, closed },
                history);                                                // record 值相等：类型、时刻、优先级、操作员一个不丢
        }

        /// <summary>操作员闭环往返保真（#26）：派生型闭环带操作员名原样读回，
        /// 事后查「这条 RTN 是谁确认闭环的」有答案。</summary>
        public static void 操作员闭环_往返保真(IEventStore store)
        {
            var closed = new AlarmClosedByOperator(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(5), "王五");

            store.Append(closed);

            var found = Assert.Single(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)));
            Assert.Equal(closed, found);                                 // record 值相等：操作员一个不丢
        }

        /// <summary>两种闭环判别分立（#26）：按 AlarmClosed 查询查不到 AlarmClosedByOperator。
        /// 条件闭环无主、操作员闭环留名，分成两类是设计不是冗余，种类查询互不混入。</summary>
        public static void 查询_两种闭环_判别分立(IEventStore store)
        {
            var conditionClosed = new AlarmClosed(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(3));
            var operatorClosed = new AlarmClosedByOperator("PUMP01_STOP", HysteresisEvaluatorTests.T(4), "王五");
            store.Append(conditionClosed);
            store.Append(operatorClosed);

            Assert.Equal(
                new AlarmEvent[] { conditionClosed },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10), kinds: [typeof(AlarmClosed)]));
            Assert.Equal(
                new AlarmEvent[] { operatorClosed },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10), kinds: [typeof(AlarmClosedByOperator)]));
        }

        /// <summary>搁置动作与带期限事件都留痕（摘要 §8.10、哲学 §7 全量留痕【强制】）：
        /// 搁置（带期限和操作员）、搁置中触发的带期限升级、解除。三条原样读回，期限一个不丢。</summary>
        public static void 搁置事件与带期限升级_往返保真(IEventStore store)
        {
            var shelved = new AlarmShelved(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(1), HysteresisEvaluatorTests.T(31), "李四");
            var raised = new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High, ShelvedUntil: HysteresisEvaluatorTests.T(31));
            var unshelved = new AlarmUnshelved(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(3), "王五");

            store.Append(shelved);
            store.Append(raised);
            store.Append(unshelved);

            Assert.Equal(
                new AlarmEvent[] { shelved, raised, unshelved },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)));
        }

        /// <summary>时间窗过滤（验收 from/to）：窗外的事件不可见，窗界上的事件可见（闭区间）。</summary>
        public static void 查询_窗口外不可见(IEventStore store)
        {
            store.Append(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High));

            Assert.Empty(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(1)));    // 窗前
            Assert.Empty(store.Query(HysteresisEvaluatorTests.T(3), HysteresisEvaluatorTests.T(10)));   // 窗后
            Assert.Single(store.Query(HysteresisEvaluatorTests.T(2), HysteresisEvaluatorTests.T(10)));  // 下界恰含
            Assert.Single(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(2)));   // 上界恰含
        }

        /// <summary>位号过滤（验收 tag）：两个位号的事件，指定一个只见一个。</summary>
        public static void 查询_按位号过滤(IEventStore store)
        {
            store.Append(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High));
            store.Append(new AlarmAutoReturned("PUMP01_STOP", HysteresisEvaluatorTests.T(4)));

            var found = store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10), tag: "PUMP01_STOP");
            Assert.Equal("PUMP01_STOP", Assert.Single(found).Tag);
        }

        /// <summary>查询按写入先后返回：append-only 存储，写入次序就是履历次序。
        /// 条件事件带事实时间戳，操作员事件带操作时的时钟读数，两种时间源合不成一个可靠的全序，
        /// 所以查询窗口只管筛选、不负责排序。乱序时刻追加的事件按追加次序读回，存储不重排。</summary>
        public static void 查询_按落库先后返回(IEventStore store)
        {
            store.Append(new AlarmClosed(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(5)));
            store.Append(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High));
            store.Append(new AlarmAcknowledged(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(3), "李四"));

            Assert.Equal(
                new[] { (nameof(AlarmClosed), HysteresisEvaluatorTests.T(5)),
                        (nameof(AlarmRaised), HysteresisEvaluatorTests.T(2)),
                        (nameof(AlarmAcknowledged), HysteresisEvaluatorTests.T(3)) },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10))
                     .Select(e => (e.GetType().Name, e.At)));
        }

        /// <summary>只进不改（验收项）：同值事件重复追加，两条都留。
        /// 没有 upsert 语义，写进一行就是一行；改历史等于毁证据（哲学 §3.5、§11 警示 13）。</summary>
        public static void 只进不改_重复追加_两条都留(IEventStore store)
        {
            var raised = new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High);

            store.Append(raised);
            store.Append(raised); // 同值再进一行：追加是唯一入口，没有「覆盖旧记录」这条路

            Assert.Equal(2, store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)).Count);
        }

        // ══════════════ #18：种类过滤（回放按类型筛，不把整窗数据搬进内存） ══════════════

        /// <summary>种类过滤（#18）：混合存放的事件里按类型筛选，只返回指定的，写入先后照旧。
        /// 回放扫描（#16）靠它在库里筛，避免把百万行全部搬进内存。</summary>
        public static void 查询_按种类过滤(IEventStore store)
        {
            store.Append(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(1), AlarmPriority.High));
            var shelved = new AlarmShelved(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), HysteresisEvaluatorTests.T(32), "李四");
            store.Append(shelved);
            store.Append(new AlarmClosed(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(3)));
            var unshelved = new AlarmUnshelved(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(4), "王五");
            store.Append(unshelved);

            Assert.Equal(
                new AlarmEvent[] { shelved, unshelved },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10),
                    kinds: [typeof(AlarmShelved), typeof(AlarmUnshelved)]));   // 次序仍是写入先后
        }

        /// <summary>种类与位号叠加：两个条件一起上，交集才返回（KPI 查询两路都会用）。</summary>
        public static void 查询_种类与位号叠加(IEventStore store)
        {
            store.Append(new AlarmShelved(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), HysteresisEvaluatorTests.T(32), "李四"));
            store.Append(new AlarmShelved("PUMP01_STOP", HysteresisEvaluatorTests.T(3), HysteresisEvaluatorTests.T(33), "王五"));
            store.Append(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(4), AlarmPriority.High));

            var found = store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10),
                tag: "PUMP01_STOP", kinds: [typeof(AlarmShelved)]);
            Assert.Equal("PUMP01_STOP", Assert.Single(found).Tag);
        }

        /// <summary>空种类等于不筛类型：不指定种类的老调用语义一字不变，接口向后兼容。</summary>
        public static void 查询_空种类_全类照旧(IEventStore store)
        {
            store.Append(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High));
            store.Append(new AlarmClosed(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(3)));

            Assert.Equal(2, store.Query(
                HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10), kinds: Array.Empty<Type>()).Count);
        }

        /// <summary>名单外的类型直接抛异常：宁可失败得响亮，也不悄悄返回一份空结果（与 Append 收到未知事件同口径）。</summary>
        public static void 查询_未知种类_校验即抛(IEventStore store)
        {
            Assert.Throws<NotSupportedException>(
                () => store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10), kinds: [typeof(string)]));
        }

        /// <summary>接口形状钉死（验收「不存在任何改写路径」）：<see cref="IEventStore"/>
        /// 只声明 Append 与 Query 两个成员，更新和删除在类型层面就进不来，而不是实现里恰好没有。</summary>
        public static void 接口_只有Append与Query_无改写路径()
        {
            var members = typeof(IEventStore)
                .GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .OrderBy(n => n)
                .ToArray();

            Assert.Equal(new[] { "Append", "Query" }, members);
        }
    }
}
