using EFCore.Infrastructure;
using Framework.Alarm.Domain;
using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 设计时 DbContext 工厂：供 dotnet-ef 等设计时工具在没有依赖注入容器时构造 DataContext。
    /// </summary>
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        /// <summary>创建指向磁盘 SQLite 库的报警上下文，供设计期工具使用。</summary>
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlite(AppGlobals.AlarmDbFilePath);

            return new DataContext(optionsBuilder.Options);
        }
    }

    /// <summary>
    /// 报警数据库上下文：档案（MADB）与事件历史同库（Alarm\AlarmDomain.db）。
    /// 与配方库（Recipe.Infrastructure.DataContext）、设备库（DeviceDataContext）、日志库各自独立。
    /// 家族持久化体系（ADR-0008）：本工程是报警家族唯一知道数据库的地方，
    /// 实体与接口住 Framework.Alarm.Domain（#20），引擎不直连数据库。
    /// </summary>
    public class DataContext : DbContext
    {
        /// <summary>构造：注入上下文选项（生产由宿主按全局路径提供，测试可传内存库选项）。</summary>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>档案表：MADB 每行一条报警的出生档案（位号主键）。</summary>
        public DbSet<AlarmDefinition> AlarmDefinitions => Set<AlarmDefinition>();

        /// <summary>事件历史表：append-only，TPH 一张表收全部事件族。</summary>
        public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();

        /// <summary>调试模式下按开关输出 SQL 日志与详细错误（#30：设 EF_SQL_LOG=1 才开，四个家族同款）；
        /// 未显式传入选项时，默认落到磁盘 SQLite 库。</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.UseDiagnosticSqlLogging();   // #30：设 EF_SQL_LOG=1 才开，四个家族同款
#endif
            // 仅当未显式传入选项（如测试传入内存库）时才落到磁盘库。
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(AppGlobals.AlarmDbFilePath);
            }

            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>应用本程序集内全部实体类型配置，再执行基类模型构建。</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
