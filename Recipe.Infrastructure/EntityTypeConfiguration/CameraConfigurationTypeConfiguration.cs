using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 相机配置实体的 EF 映射配置。
    /// </summary>
    public class CameraConfigurationTypeConfiguration : IEntityTypeConfiguration<CameraConfiguration>
    {
        /// <summary>配置相机配置实体映射：Id 由数据库生成（自增）。</summary>
        public void Configure(EntityTypeBuilder<CameraConfiguration> builder)
        {

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasComment("Data unique identifier.");
        }
    }
}
