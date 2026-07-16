using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Recipe.Domain
{
    public class RecipeManagerTypeConfiguration : IEntityTypeConfiguration<RecipeManager>
    {
        #region IEntityTypeConfiguration

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
