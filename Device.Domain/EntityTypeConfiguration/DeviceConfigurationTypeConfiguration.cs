using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FrameWork.Device;

namespace Device.Domain
{
    /// <summary>
    /// DeviceConfiguration 实体 Fluent API 配置
    /// </summary>
    public class DeviceConfigurationTypeConfiguration : IEntityTypeConfiguration<DeviceConfiguration>
    {
        public void Configure(EntityTypeBuilder<DeviceConfiguration> builder)
        {
            // 表名
            builder.ToTable("DeviceConfigurations");

            // 主键
            builder.HasKey(e => e.Id);

            // DeviceCode 唯一索引，快速查找
            builder.HasIndex(e => e.DeviceCode)
                .IsUnique()
                .HasDatabaseName("IX_DeviceConfigurations_DeviceCode");

            // 按设备类型筛选的索引
            builder.HasIndex(e => new { e.DeviceType, e.IsEnabled })
                .HasDatabaseName("IX_DeviceConfigurations_Type_Enabled");

            // DeviceCode 必填，最大长度 50
            builder.Property(e => e.DeviceCode)
                .IsRequired()
                .HasMaxLength(50);

            // DeviceName 必填，最大长度 100
            builder.Property(e => e.DeviceName)
                .IsRequired()
                .HasMaxLength(100);

            // DeviceType 以 int 存储
            builder.Property(e => e.DeviceType)
                .IsRequired()
                .HasConversion<int>();

            // AssemblyFileName 最大长度 200
            builder.Property(e => e.AssemblyFileName)
                .IsRequired()
                .HasMaxLength(200);

            // ClassFullName 最大长度 200
            builder.Property(e => e.ClassFullName)
                .IsRequired()
                .HasMaxLength(200);

            // ConnectionParams 以 nvarchar(max) 存储 JSON
            builder.Property(e => e.ConnectionParams)
                .HasMaxLength(4000)
                .HasDefaultValue("{}");

            // IsEnabled 默认为 true
            builder.Property(e => e.IsEnabled)
                .HasDefaultValue(true);

            // SortOrder 默认为 0
            builder.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            // Remark 可空，最大 500
            builder.Property(e => e.Remark)
                .HasMaxLength(500)
                .IsRequired(false);
        }
    }
}
