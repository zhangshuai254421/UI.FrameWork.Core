using Framework.Core.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 宿主建库一口（#22）：宿主启动时调 <see cref="Initialize"/>，报警库在这里出生——
    /// 建目录（SQLite 不替人挖目录）→ 迁移建表 → WAL 就位。存储构造不再建库，
    /// 谁来通电谁先点灯：与配方/设备/日志三库的宿主建库（UseDatabaseEnsureCreated）同一时刻、同一宿主，
    /// 只是报警库走迁移——90 天保留期要跨版本存活（哲学 §8.1/§12 #3），EnsureCreated 建出的库套不上迁移（#21 拍板）。
    /// 连接模型拍板（#21 评审留痕移交）：两存储各开连接打同一库文件（连接字符串而非共享连接实例），
    /// WAL 让读写不互堵（UI 查详情读 vs 引擎落库写），写写相撞由 Microsoft.Data.Sqlite
    /// 命令超时（默认 30 秒）的 busy 重试兜底——小事务写入，30 秒绰绰有余。
    /// </summary>
    public static class AlarmDatabase
    {
        /// <summary>
        /// 建库（幂等，重启宿主的日常）：目录缺了挖目录，库缺了走迁移建表，WAL 缺了补上
        /// （journal_mode 写进库文件持久生效）。初始化失败就地炸响——哑火建库等于把
        /// 「无档案不报警」的数据前提留给运行期才发现。
        /// </summary>
        /// <param name="connectionString">库连接串；缺省落应用全局路径（Alarm\AlarmDomain.db）。</param>
        public static void Initialize(string? connectionString = null)
        {
            connectionString ??= AppGlobals.AlarmDbFliePath;

            var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
            var fullPath = Path.GetFullPath(dataSource); // 相对路径按当前目录展开，目录才挖得准
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var context = new DataContext(
                new DbContextOptionsBuilder<DataContext>().UseSqlite(connectionString).Options);
            context.Database.Migrate();
            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }
    }
}
