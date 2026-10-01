using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 配方参数基类实体的 EF 映射配置。
    /// </summary>
    public class RecipeParameterTypeConfiguration : IEntityTypeConfiguration<RecipeParameter>
    {
        /// <summary>配置配方参数基类映射：启用 TPC（表按具体类型）映射策略，Id 由数据库生成。</summary>
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
