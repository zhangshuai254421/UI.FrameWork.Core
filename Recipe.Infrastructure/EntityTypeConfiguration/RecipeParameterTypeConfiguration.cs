using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    public class RecipeParameterTypeConfiguration : IEntityTypeConfiguration<RecipeParameter>
    {
        public void Configure(EntityTypeBuilder<RecipeParameter> builder)
        {
            builder
                .HasKey(x => x.Id);

            // 【关键】在基类上启用 TPC 映射策略
            builder.UseTpcMappingStrategy();
            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasComment("Data unique identifier.");
        }
    }
}
