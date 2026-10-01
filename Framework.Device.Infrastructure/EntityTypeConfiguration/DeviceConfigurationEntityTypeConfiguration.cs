using Framework.Device.Domain;
using Framework.Device.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Framework.Device.Infrastructure.EntityTypeConfiguration
{
    public class DeviceConfigurationEntityTypeConfiguration : IEntityTypeConfiguration<DeviceConfigurationEntity>
    {
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
