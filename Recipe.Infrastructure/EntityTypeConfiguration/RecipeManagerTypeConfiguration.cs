using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipe.Domain;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 配方管理器实体的 EF 映射配置。
    /// </summary>
    public class RecipeManagerTypeConfiguration : IEntityTypeConfiguration<RecipeManager>
    {
        #region IEntityTypeConfiguration

        /// <summary>配置配方管理器实体映射：CurrentRecipe 自动加载，并约束全表仅允许 Id 为 1 的单行。</summary>
        public void Configure(EntityTypeBuilder<RecipeManager> builder)
        {
            builder
                .HasKey(x => x.Id);

            // ★ 关键：全局自动加载
            builder.Navigation(x => x.CurrentRecipe)
              .AutoInclude();  // ← 每次查询 RecipeManager 都会自动加载 CurrentRecipe

            builder.ToTable(tb => tb.HasCheckConstraint("CK_RecipeManager_SingleRow", "[Id] = 1"));
        }

        #endregion
    }
}
