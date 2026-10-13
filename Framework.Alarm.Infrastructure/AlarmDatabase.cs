using Framework.Core.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 宿主建库入口（#22）：宿主启动时调 <see cref="Initialize"/>，报警库在这里创建——
    /// 建目录（SQLite 不会自己建目录）→ 迁移建表 → 开 WAL。存储构造不再建库。
    /// 与配方、设备、日志三个库的宿主建库（UseDatabaseEnsureCreated）在同一时刻、同一宿主做，
    /// 只是报警库走迁移——90 天保留期要跨版本存活（哲学 §8.1/§12 #3），
    /// EnsureCreated 建出的库套不上迁移（#21 拍板）。
    /// 连接模型（#21 评审留痕移交）：两个存储各开连接访问同一个库文件（用连接字符串而非共享连接实例），
    /// WAL 让读写不互堵（UI 查详情读 vs 引擎落库写）；写写相撞由 Microsoft.Data.Sqlite
    /// 的命令超时（默认 30 秒）自动 busy 重试兜底——小事务写入，30 秒绰绰有余。
    /// </summary>
    public static class AlarmDatabase
    {
        /// <summary>
        /// 建库（幂等，重启宿主的日常）：目录缺了建目录，库缺了走迁移建表，WAL 没开就补上
        /// （journal_mode 写进库文件持久生效）。初始化失败就地抛异常——静默失败的建库等于把
        /// 「无档案不报警」的数据前提问题拖到运行期才暴露。
        /// </summary>
        /// <param name="connectionString">库连接串；缺省用应用全局路径（Alarm\AlarmDomain.db）。</param>
        public static void Initialize(string? connectionString = null)
        {
            connectionString ??= AppGlobals.AlarmDbFilePath;

            var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
            var fullPath = Path.GetFullPath(dataSource); // 相对路径按当前目录展开，目录才建得准
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var context = new DataContext(
                new DbContextOptionsBuilder<DataContext>().UseSqlite(connectionString).Options);
            context.Database.Migrate();
            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }
    }
}
