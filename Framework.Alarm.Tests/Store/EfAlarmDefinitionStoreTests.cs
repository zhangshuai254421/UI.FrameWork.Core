using Framework.Alarm.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// EF 档案存储过契约，与内存替身同一套断言（持久化体系见 ADR-0008，#21 接替裸 SQLite）。
    /// 测试用 :memory: 库：连接要先开好再交给 EF，因为 :memory: 库随连接存活，EF 自己开关连接库就没了；
    /// 建表走 EfTestDb，#22 起建库不归存储构造管。
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
        public void 增_操作动作缺_校验即抛() => AlarmDefinitionStoreContract.增_操作动作缺_校验即抛(NewStore());

        [Fact]
        public void 增_触发延时逾MTR_被拒() => AlarmDefinitionStoreContract.增_触发延时逾MTR_被拒(NewStore());

        [Fact]
        public void 改_操作动作缺_校验即抛() => AlarmDefinitionStoreContract.改_操作动作缺_校验即抛(NewStore());

        [Fact]
        public void 改_写失败_残留不影响后续操作()
        {
            var options = EfTestDb.MigratedOptions();
            var store = new AlarmDefinitionStore(options);
            store.Add(AlarmDefinitionTests.Sample());

            // 注入一次性写失败：UPDATE 一律 ABORT 的触发器（旁路上下文走同一条内存库连接）
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

            // 残留断言（#27）：上一次失败的残留不许影响下一次操作。同位号 Add 必须走到库层撞主键冲突、
            // 翻译成契约约定的 ArgumentException；残留若还留在 EF 跟踪缓存里，就轮不到库层、会抛契约外的 InvalidOperationException
            Assert.Throws<ArgumentException>(() => store.Add(AlarmDefinitionTests.Sample()));

            store.Update(AlarmDefinitionTests.Sample() with { Limit = 260 }); // 触发器撤了，改恢复如常
            Assert.Equal(260, store.Find("HEAT01_TEMP_HI")!.Limit);           // 读的是库态不是跟踪残留
        }

        [Fact]
        public void 删_写失败_残留不影响后续操作()
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
            Assert.Throws<ArgumentException>(() => store.Add(AlarmDefinitionTests.Sample())); // 残留不影响后续（#27）：主键冲突走库层翻译
            Assert.True(store.Delete("HEAT01_TEMP_HI"));                        // 恢复后删得掉
            Assert.Null(store.Find("HEAT01_TEMP_HI"));
        }

        /// <summary>每测一个独立内存库；连接存活期内库有效，测试结束随连接释放。</summary>
        private static AlarmDefinitionStore NewStore() => new(EfTestDb.MigratedOptions());
    }
}
