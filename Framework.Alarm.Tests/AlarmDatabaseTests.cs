using Framework.Alarm.Domain;
using Framework.Alarm.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 宿主建库口联测（#22）：<see cref="AlarmDatabase.Initialize"/> 一口完成「建目录 → 迁移建表 → WAL 就位」。
    /// 建表走迁移不走 EnsureCreated（#21 拍板：90 天保留期跨版本存活，哲学 §8.1/§12 #3）；
    /// WAL 是两存储各开连接打同一库文件的并发拍板（#21 评审留痕移交 #22）——读写不互堵，
    /// 写写相撞由 Microsoft.Data.Sqlite 命令超时（默认 30 秒）的 busy 重试兜底。
    /// </summary>
    public class AlarmDatabaseTests
    {
        [Fact]
        public void 初始化_全新目录_库表就位存储即开即用()
        {
            var connectionString = NewTempDbConnectionString();
            try
            {
                AlarmDatabase.Initialize(connectionString); // 目录不存在：建库口自己挖目录

                var store = new AlarmDefinitionStore(EfOptions(connectionString));

                Assert.Empty(store.LoadAll()); // 表在：全量加载空手而归，不炸「no such table」
            }
            finally
            {
                Cleanup(connectionString);
            }
        }

        [Fact]
        public void 初始化后_journal_mode_已是_WAL()
        {
            var connectionString = NewTempDbConnectionString();
            try
            {
                AlarmDatabase.Initialize(connectionString);

                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA journal_mode;";

                Assert.Equal("wal", command.ExecuteScalar());
            }
            finally
            {
                Cleanup(connectionString);
            }
        }

        [Fact]
        public void 初始化_幂等_二度跑_数据还在()
        {
            var connectionString = NewTempDbConnectionString();
            try
            {
                AlarmDatabase.Initialize(connectionString);
                var events = new EventStore(EfOptions(connectionString));
                events.Append(new AlarmRaised("HEAT01", DateTimeOffset.Now, AlarmPriority.High));

                AlarmDatabase.Initialize(connectionString); // 再跑一遍（重启宿主的日常）：迁移幂等，不重建库

                Assert.Single(events.Query(DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
            }
            finally
            {
                Cleanup(connectionString);
            }
        }

        /// <summary>指向临时目录里全新库文件的连接串（目录一并不存在，考「建库口挖目录」）。</summary>
        private static string NewTempDbConnectionString()
            => $"Data Source={Path.Combine(Path.GetTempPath(), "alarm-db-tests", Guid.NewGuid().ToString("N"), "AlarmDomain.db")}";

        /// <summary>与生产宿主同款：连接字符串选项（每口各开连接，不共享连接实例）。</summary>
        private static DbContextOptions<DataContext> EfOptions(string connectionString)
            => new DbContextOptionsBuilder<DataContext>().UseSqlite(connectionString).Options;

        /// <summary>测试库用完即拆；连接池里还攥着文件句柄，先清池再删目录，删不掉随它去（临时目录）。</summary>
        private static void Cleanup(string connectionString)
        {
            try
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(Path.GetDirectoryName(
                    new SqliteConnectionStringBuilder(connectionString).DataSource)!, recursive: true);
            }
            catch
            {
                // 临时目录删不净无伤大雅，不让清理噪音盖过断言
            }
        }
    }
}
