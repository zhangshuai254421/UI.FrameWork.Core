using Log.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Infrastructure
{
    public class SerilogHistoryTypeConfiguration : IEntityTypeConfiguration<SerilogHistory>
    {
        public void Configure(EntityTypeBuilder<SerilogHistory> builder)
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
