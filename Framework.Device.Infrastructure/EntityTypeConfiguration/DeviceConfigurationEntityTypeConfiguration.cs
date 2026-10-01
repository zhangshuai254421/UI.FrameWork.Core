using Framework.Device.Domain;
using Framework.Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Framework.Device.Domain.EntityTypeConfiguration
{
    /// <summary>设备配置实体的数据库映射：表名、主键、字段长度与内置种子数据。</summary>
    public class DeviceConfigurationEntityTypeConfiguration : IEntityTypeConfiguration<DeviceConfigurationEntity>
    {
        /// <summary>配置表名 DeviceConfiguration、主键 Id 与字段长度，并内置两台种子设备（一台 TCP 相机、一台 COM 运动卡）。</summary>
        public void Configure(EntityTypeBuilder<DeviceConfigurationEntity> builder)
        {
            builder.ToTable("DeviceConfiguration");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasMaxLength(64).HasComment("设备逻辑标识（主键）");
            builder.Property(x => x.Vendor).HasMaxLength(128);

            builder.HasData(
                new DeviceConfigurationEntity
                {
                    Id = "CameraA",
                    Kind = DeviceKind.Camera,
                    Vendor = "HikVision",
                    ConnectionType = ConnectionType.Tcp,
                    IpAddress = "192.168.1.64",
                    Port = 8000,
                },
                new DeviceConfigurationEntity
                {
                    Id = "MotionCard1",
                    Kind = DeviceKind.MotionControlCard,
                    Vendor = "Leisai",
                    ConnectionType = ConnectionType.Com,
                    PortName = "COM1",
                });
        }
    }
}
