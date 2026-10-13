using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 内存档案存储（测试替身）过契约，与 SQLite 实现同一套断言。
    /// 照 FakeCamera 惯例，替身住测试工程。
    /// </summary>
    public class InMemoryAlarmDefinitionStoreTests
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

        private static InMemoryAlarmDefinitionStore NewStore() => new();
    }
}
