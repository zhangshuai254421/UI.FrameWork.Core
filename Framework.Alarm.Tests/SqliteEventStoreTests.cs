using Microsoft.Data.Sqlite;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// SQLite 事件存储过契约——与内存替身同一套断言（存储 v1 直接 SQLite，ADR-0009 规则 5）。
    /// 测试用 :memory: 库，连接即库、不落盘。
    /// </summary>
    public class SqliteEventStoreTests
    {
        [Fact]
        public void 追加_查询可见_原样读回() => EventStoreContract.追加_查询可见_原样读回(NewStore());

        [Fact]
        public void 四种事件_往返保真() => EventStoreContract.四种事件_往返保真(NewStore());

        [Fact]
        public void 搁置事件与带标升级_往返保真() => EventStoreContract.搁置事件与带标升级_往返保真(NewStore());

        /// <summary>旧库迁移（加列）：上一版本的表结构（无 ShelvedUntil 列）接上新代码——
        /// 幂等补列后照常落库读回带标事件，老数据不丢、新标不缺（历史库全量含带标事件，摘要 §8.10）。</summary>
        [Fact]
        public void 旧库无搁置标列_接新代码_补列照常往返()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var command = connection.CreateCommand()) // 手工立上一版本的旧表：没有 ShelvedUntil 列
            {
                var legacyTicks = HysteresisEvaluatorTests.T(5).UtcTicks;
                command.CommandText =
                    $"""
                    CREATE TABLE AlarmEvent (
                        Seq INTEGER PRIMARY KEY AUTOINCREMENT,
                        Kind TEXT NOT NULL,
                        Tag TEXT NOT NULL,
                        At TEXT NOT NULL,
                        UtcTicks INTEGER NOT NULL,
                        Priority TEXT,
                        Operator TEXT
                    );
                    INSERT INTO AlarmEvent (Kind, Tag, At, UtcTicks, Priority)
                        VALUES ('AlarmClosed', 'HEAT01_TEMP_HI', '{HysteresisEvaluatorTests.T(5).ToString("O")}', {legacyTicks}, NULL)
                    """;
                command.ExecuteNonQuery();
            }

            var store = new SqliteEventStore(connection); // 构造即迁移：补列不毁旧数据

            var legacy = Assert.Single(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)));
            Assert.IsType<AlarmClosed>(legacy);           // 旧数据原样读回

            var raised = new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High, ShelvedUntil: HysteresisEvaluatorTests.T(31));
            store.Append(raised);                          // 新标照常落库

            Assert.Equal(
                new AlarmEvent[] { legacy, raised },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)));
        }

        [Fact]
        public void 查询_窗口外不可见() => EventStoreContract.查询_窗口外不可见(NewStore());

        [Fact]
        public void 查询_按位号过滤() => EventStoreContract.查询_按位号过滤(NewStore());

        [Fact]
        public void 查询_按落库先后返回() => EventStoreContract.查询_按落库先后返回(NewStore());

        [Fact]
        public void 只进不改_重复追加_两条都留() => EventStoreContract.只进不改_重复追加_两条都留(NewStore());

        [Fact]
        public void 接口_只有Append与Query_无改写路径() => EventStoreContract.接口_只有Append与Query_无改写路径();

        /// <summary>
        /// 端到端联测（验收「接线后每一拍在库」走真存储）：引擎 → EventHistory → SQLite → 查询。
        /// 六拍剧本五条事件全在盘上（:memory: 库），重启连接即失——落盘持久性是宿主选库文件的事，
        /// 本测钉的是「真库也吃得下引擎流」。
        /// </summary>
        [Fact]
        public void 端到端_引擎流经接线进真库_六拍全在()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var store = new SqliteEventStore(connection);

            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            _ = new EventHistory(store, engine);

            SixBeatScript.Run(source, engine, clock);

            Assert.Equal(
                SixBeatScript.ExpectedEventTypes,
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(13)).Select(e => e.GetType().Name));
        }

        /// <summary>每测一个独立内存库；连接存活期内库有效，测试结束随连接释放。</summary>
        private static SqliteEventStore NewStore()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return new SqliteEventStore(connection);
        }
    }
}
