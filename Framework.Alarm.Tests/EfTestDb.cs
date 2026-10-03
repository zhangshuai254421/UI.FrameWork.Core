using Framework.Alarm.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// EF 存储测试的公共库脚手架（#22）：开好的 :memory: 连接（库随连接存活）+ 过了迁移的选项。
    /// 建库不归存储构造（归宿主 AlarmDatabase.Initialize），测试自建自用——同走迁移不走
    /// EnsureCreated，与生产同一条建表路径；连接不经手释放（测试进程生命周期内库有效）。
    /// </summary>
    internal static class EfTestDb
    {
        /// <summary>建好表的内存库选项：连接先开再交给 EF，迁移走生产同款路径。</summary>
        public static DbContextOptions<DataContext> MigratedOptions()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;

            using var context = new DataContext(options);
            context.Database.Migrate();

            return options;
        }
    }
}
