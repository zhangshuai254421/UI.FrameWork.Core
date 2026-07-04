using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Recipe.Domain
{
    public class RecipeTypeConfiguration : IEntityTypeConfiguration<Recipe>
    {
        #region IEntityTypeConfiguration

        public void Configure(EntityTypeBuilder<Recipe> builder)
        {
            builder
                .HasKey(x => x.Id);

            //builder.HasIndex(p => new
            //{
            //    p.RecipeName,
            //    p.MachineName
            //}).IsUnique();

            //当前实体（主表）和 FlowParameter 是”一对多”关系，并且：删除主表记录时，数据库会自动级联删除子表记录。
            builder
                .HasMany(x => x.Parameters)
                .WithOne(p => p.Recipe)
                .OnDelete(DeleteBehavior.Cascade);

            //查询的时候自动填充
            builder.Navigation(p => p.Parameters).AutoInclude();

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasComment(“Data unique identifier.”);
        }

        #endregion
    }


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

            builder.ToTable(tb => tb.HasCheckConstraint(“CK_RecipeManager_SingleRow”, “[Id] = 1”));
        }

        #endregion
    }
}
