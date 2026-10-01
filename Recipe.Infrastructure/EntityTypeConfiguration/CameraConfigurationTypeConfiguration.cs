using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipe.Domain;

namespace Recipe.Infrastructure
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
