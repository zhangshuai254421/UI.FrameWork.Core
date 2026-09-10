using Framework.Core.Common;
using Framework.Core.CustomAttribute;
using Framework.Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

[assembly: DefaultDbContext(typeof(Framework.Device.Domain.DeviceDataContext))]

namespace Framework.Device.Domain
{
    /// <summary>
    /// 设备数据库上下文：持久化设备级配置（Device\DeviceDomain.db）。
    /// 与配方库（Recipe.Domain.DataContext）、日志库（Log.Domain.DataContext）各自独立。
    /// </summary>
    public partial class DeviceDataContext : DbContext
    {
        public DeviceDataContext(DbContextOptions<DeviceDataContext> options) : base(options)
        {
        }

        public DbSet<DeviceConfigurationEntity> DeviceConfigurations => Set<DeviceConfigurationEntity>();

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
