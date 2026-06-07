using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Recipe.Domain
{
    public class RecipeParameterTypeConfiguration : IEntityTypeConfiguration<RecipeParameter>
    {
        public void Configure(EntityTypeBuilder<RecipeParameter> builder)
        {
            builder
                .HasKey(x => x.Id);


            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasComment("Data unique identifier.");
        }
    }
}
