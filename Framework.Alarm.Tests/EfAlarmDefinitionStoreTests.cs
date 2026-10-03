using Framework.Alarm.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// EF 档案存储过契约——与内存替身同一套断言（家族持久化体系，ADR-0008；#21 接棒裸 SQLite）。
    /// 测试用 :memory: 库：连接先开好再交给 EF（:memory: 库随连接存活，EF 自己开关连接库就蒸发了）；
    /// 建表自建（EfTestDb，#22 起建库不归存储构造）。
    /// </summary>
    public class EfAlarmDefinitionStoreTests
    {
        [Fact]
        public void 增_新档_查回原档() => AlarmDefinitionStoreContract.增_新档_查回原档(NewStore());

        [Fact]
        public void 查_无此档_返回空() => AlarmDefinitionStoreContract.查_无此档_返回空(NewStore());

        [Fact]
        public void 改_同位号_整行覆盖() => AlarmDefinitionStoreContract.改_同位号_整行覆盖(NewStore());

        [Fact]
        public void 删_删后查无() => AlarmDefinitionStoreContract.删_删后查无(NewStore());

        [Fact]
        public void 全量加载_三条档_条目齐全() => AlarmDefinitionStoreContract.全量加载_三条档_条目齐全(NewStore());

        [Fact]
        public void 增_重复位号_被拒() => AlarmDefinitionStoreContract.增_重复位号_被拒(NewStore());

        [Fact]
        public void 增_操作动作缺_当庭退回() => AlarmDefinitionStoreContract.增_操作动作缺_当庭退回(NewStore());

        [Fact]
        public void 增_触发延时逾MTR_被拒() => AlarmDefinitionStoreContract.增_触发延时逾MTR_被拒(NewStore());

        [Fact]
        public void 改_操作动作缺_当庭退回() => AlarmDefinitionStoreContract.改_操作动作缺_当庭退回(NewStore());

        [Fact]
        public void 改_写失败_残留不毒化后续操作()
        {
            var options = EfTestDb.MigratedOptions();
            var store = new AlarmDefinitionStore(options);
            store.Add(AlarmDefinitionTests.Sample());

            // 注入一次性写失败：UPDATE 全 ABORT 的触发器（旁路上下文走同一条内存库连接）
            using (var raw = new DataContext(options))
            {
                raw.Database.ExecuteSqlRaw(
                    "CREATE TRIGGER fail_update BEFORE UPDATE ON AlarmDefinition BEGIN SELECT RAISE(ABORT, 'injected'); END;");
            }

            Assert.Throws<DbUpdateException>(                   // 写失败原样外抛（只有撞号才翻译）
                () => store.Update(AlarmDefinitionTests.Sample() with { Limit = 260 }));

            using (var raw = new DataContext(options))
            {
                raw.Database.ExecuteSqlRaw("DROP TRIGGER fail_update;");
            }

            // 毒化断言（#27）：失败残留不许带进下一口——同位号 Add 必须走到库层撞号、
            // 翻译成契约约定的 ArgumentException；残留跟踪在-context 里则轮不到库、抛契约外的 InvalidOperationException
            Assert.Throws<ArgumentException>(() => store.Add(AlarmDefinitionTests.Sample()));

            store.Update(AlarmDefinitionTests.Sample() with { Limit = 260 }); // 触发器撤了，改恢复如常
            Assert.Equal(260, store.Find("HEAT01_TEMP_HI")!.Limit);           // 读的是库态不是跟踪残留
        }

        [Fact]
        public void 删_写失败_残留不毒化后续操作()
        {
            var options = EfTestDb.MigratedOptions();
            var store = new AlarmDefinitionStore(options);
            store.Add(AlarmDefinitionTests.Sample());

            using (var raw = new DataContext(options))
            {
                raw.Database.ExecuteSqlRaw(
                    "CREATE TRIGGER fail_delete BEFORE DELETE ON AlarmDefinition BEGIN SELECT RAISE(ABORT, 'injected'); END;");
            }

            Assert.Throws<DbUpdateException>(() => store.Delete("HEAT01_TEMP_HI")); // 删除失败原样外抛

            using (var raw = new DataContext(options))
            {
                raw.Database.ExecuteSqlRaw("DROP TRIGGER fail_delete;");
            }

            Assert.NotNull(store.Find("HEAT01_TEMP_HI"));                       // 删失败：档还在
            Assert.Throws<ArgumentException>(() => store.Add(AlarmDefinitionTests.Sample())); // 残留不毒化（#27）：撞号走库层翻译
            Assert.True(store.Delete("HEAT01_TEMP_HI"));                        // 恢复后删得掉
            Assert.Null(store.Find("HEAT01_TEMP_HI"));
        }

        /// <summary>每测一个独立内存库；连接存活期内库有效，测试结束随连接释放。</summary>
        private static AlarmDefinitionStore NewStore() => new(EfTestDb.MigratedOptions());
    }
}
