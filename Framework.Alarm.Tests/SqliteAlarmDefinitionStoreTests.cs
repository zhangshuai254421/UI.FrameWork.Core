using Framework.Alarm;
using Microsoft.Data.Sqlite;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// SQLite 档案存储过契约——与内存替身同一套断言（存储 v1 直接 SQLite，ADR-0009 规则 5）。
    /// 测试用 :memory: 库，连接即库、不落盘。
    /// </summary>
    public class SqliteAlarmDefinitionStoreTests
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

        /// <summary>每测一个独立内存库；连接存活期内库有效，测试结束随连接释放。</summary>
        private static SqliteAlarmDefinitionStore NewStore()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return new SqliteAlarmDefinitionStore(connection);
        }
    }
}
