using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace Device.Domain
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlite(AppGlobals.DeviceDbFliePath);
            return new DataContext(optionsBuilder.Options);
        }
    }

    public partial class DataContext : DbContext
    {
        #region DbSet

        public DbSet<DeviceConfiguration> DeviceConfigurations => Set<DeviceConfiguration>();

        #endregion

        #region 构造函数

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        #endregion

        #region 方法

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
#endif
            optionsBuilder.UseSqlite(AppGlobals.DeviceDbFliePath);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            // 种子数据：示例设备配置（启用状态可根据需要调整）
            modelBuilder.Entity<DeviceConfiguration>().HasData(
                new DeviceConfiguration(
                    "CAM-DEMO",
                    "演示相机",
                    FrameWork.Device.DeviceType.Camera,
                    "DemoCamera.dll",
                    "DemoCamera.DemoCamera",
                    "{\"IP\":\"127.0.0.1\",\"Port\":8000}",
                    isEnabled: false,  // 默认禁用，用户按需启用
                    sortOrder: 1
                ),
                new DeviceConfiguration(
                    "MOT-DEMO",
                    "演示运动控制卡",
                    FrameWork.Device.DeviceType.MotionCard,
                    "DemoMotionCard.dll",
                    "DemoMotionCard.DemoMotionCard",
                    "{\"CardNo\":0,\"AxisCount\":4}",
                    isEnabled: false,
                    sortOrder: 2
                )
            );

            base.OnModelCreating(modelBuilder);
        }

        #endregion
    }
}
