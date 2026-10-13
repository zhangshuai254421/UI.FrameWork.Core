using Framework.Alarm.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// EF 事件存储过契约，与内存替身同一套断言（持久化体系见 ADR-0008，#21 接替裸 SQLite）。
    /// 测试用 :memory: 库：连接要先开好再交给 EF，因为 :memory: 库随连接存活，EF 自己开关连接库就没了；
    /// 建表走 EfTestDb，#22 起建库不归存储构造管。
    /// 旧库加列迁移用例随裸 SQLite 一并退役：裸 SQLite 的旧库（Seq 表、列集不同）是外来 schema，
    /// 迁移发现库里没有迁移史会走 InitialCreate，撞上已有表直接报错，响亮失败，不再静默补列后继续写。
    /// </summary>
    public class EfEventStoreTests
    {
        [Fact]
        public void 追加_查询可见_原样读回() => EventStoreContract.追加_查询可见_原样读回(NewStore());

        [Fact]
        public void 四种事件_往返保真() => EventStoreContract.四种事件_往返保真(NewStore());

        [Fact]
        public void 操作员闭环_往返保真() => EventStoreContract.操作员闭环_往返保真(NewStore());

        [Fact]
        public void 查询_两种闭环_判别分立() => EventStoreContract.查询_两种闭环_判别分立(NewStore());

        [Fact]
        public void 搁置事件与带期限升级_往返保真() => EventStoreContract.搁置事件与带期限升级_往返保真(NewStore());

        [Fact]
        public void 查询_窗口外不可见() => EventStoreContract.查询_窗口外不可见(NewStore());

        [Fact]
        public void 查询_按位号过滤() => EventStoreContract.查询_按位号过滤(NewStore());

        [Fact]
        public void 查询_按落库先后返回() => EventStoreContract.查询_按落库先后返回(NewStore());

        [Fact]
        public void 查询_按种类过滤() => EventStoreContract.查询_按种类过滤(NewStore());

        [Fact]
        public void 查询_种类与位号叠加() => EventStoreContract.查询_种类与位号叠加(NewStore());

        [Fact]
        public void 查询_空种类_全类照旧() => EventStoreContract.查询_空种类_全类照旧(NewStore());

        [Fact]
        public void 查询_未知种类_校验即抛() => EventStoreContract.查询_未知种类_校验即抛(NewStore());

        [Fact]
        public void 只进不改_重复追加_两条都留() => EventStoreContract.只进不改_重复追加_两条都留(NewStore());

        [Fact]
        public void 接口_只有Append与Query_无改写路径() => EventStoreContract.接口_只有Append与Query_无改写路径();

        /// <summary>
        /// 端到端联测，验收「接线后每条事件都在库」，走真存储：引擎 → EventHistory → EF 库 → 查询。
        /// 六步剧本的五条事件全部要能查到。:memory: 库在连接关闭后就没了，落盘持久性是宿主选库文件的事，
        /// 本测要钉住的是「真库也吃得下引擎的事件流」。写库走后台队列（#31），断言前先等它写完。
        /// </summary>
        [Fact]
        public void 端到端_引擎流经接线进真库_六步全在()
        {
            var store = NewStore();

            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var history = new EventHistory(store, engine);

            SixStepScript.Run(source, engine, clock);
            history.Dispose();                               // 收尾：等后台写者把存量写完再断言（#31）

            Assert.Equal(
                SixStepScript.ExpectedEventTypes,
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(13)).Select(e => e.GetType().Name));
        }

        /// <summary>时间窗列有索引（#28）：启动回放与历史查询都按 UtcTicks BETWEEN 查询，
        /// 90 天履历没有索引就是全表扫描，成本随履历条数线性上涨。索引在模型里钉死，随迁移写进库。</summary>
        [Fact]
        public void 事件表_时间窗列有索引()
        {
            using var context = new DataContext(EfTestDb.MigratedOptions());

            var entityType = context.Model.FindEntityType(typeof(AlarmEvent));
            Assert.NotNull(entityType);
            Assert.Contains(entityType.GetIndexes(), i => i.Properties.Single().Name == "UtcTicks");
        }

        /// <summary>每测一个独立内存库；连接存活期内库有效，测试结束随连接释放。</summary>
        private static EventStore NewStore() => new(EfTestDb.MigratedOptions());
    }
}
