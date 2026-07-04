using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recipe.Domain
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        #region IDesignTimeDbContextFactory

        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            var connectionString = AppGlobals.RecipeDbFliePath;

            // 2. 根据你使用的数据库提供程序配置 (此处以 SQL Server 为例)
            optionsBuilder.UseSqlite(connectionString);

            return new DataContext(optionsBuilder.Options);
        }

        #endregion
    }

    public partial class DataContext : DbContext
    {
        #region 构造函数

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        #endregion

        #region 方法

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
#endif
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            optionsBuilder.UseSqlite(AppGlobals.RecipeDbFliePath);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            modelBuilder.Entity<Recipe>().HasData(
                new Recipe(-1, "default", "-Default-", "ZS1A")
            );
            modelBuilder.Entity<RecipeManager>().HasData(
                new RecipeManager(1, -1)  // 指向默认配方
            );

            base.OnModelCreating(modelBuilder);
        }

        #endregion
    }
}
