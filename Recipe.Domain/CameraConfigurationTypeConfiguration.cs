using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Recipe.Domain
{
    public class CameraConfigurationTypeConfiguration : IEntityTypeConfiguration<CameraConfiguration>
    {
        public void Configure(EntityTypeBuilder<CameraConfiguration> builder)
        {

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasComment("Data unique identifier.");
        }
    }
}
