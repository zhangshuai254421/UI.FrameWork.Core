using Framework.Core.Common;
using Framework.Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace Framework.Device.Infrastructure
{
    /// <summary>
    /// 设计期上下文工厂：供 EF Core 设计时工具（如生成迁移）在无依赖注入容器时构造设备上下文。
    /// </summary>
    public class DataContextFactory : IDesignTimeDbContextFactory<DeviceDataContext>
    {
        #region IDesignTimeDbContextFactory

        /// <summary>创建指向磁盘 SQLite 库的设备上下文，供设计期工具使用。</summary>
        public DeviceDataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DeviceDataContext>();
            var connectionString = AppGlobals.DeviceDbFliePath;

            // 2. 根据你使用的数据库提供程序配置 (此处以 SQL Server 为例)
            optionsBuilder.UseSqlite(connectionString);

            return new DeviceDataContext(optionsBuilder.Options);
        }

        #endregion
    }
    /// <summary>
    /// 设备数据库上下文：持久化设备级配置（Device\DeviceDomain.db）。
    /// 与配方库（Recipe.Infrastructure.DataContext）、日志库（Log.Infrastructure.DataContext）各自独立。
    /// </summary>
    public partial class DeviceDataContext : DbContext
    {
        /// <summary>构造：注入上下文选项（生产由依赖注入提供，测试可传入内存库选项）。</summary>
        public DeviceDataContext(DbContextOptions<DeviceDataContext> options) : base(options)
        {
        }

        /// <summary>设备配置表：全部设备级配置实体的访问入口。</summary>
        public DbSet<DeviceConfigurationEntity> DeviceConfigurations => Set<DeviceConfigurationEntity>();

        /// <summary>调试模式下输出 SQL 日志与详细错误；未显式传入选项时默认落到磁盘 SQLite 库。</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
#endif
            // 仅当未显式传入选项（如测试传入内存库）时才落到磁盘库。
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(AppGlobals.DeviceDbFliePath);
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
