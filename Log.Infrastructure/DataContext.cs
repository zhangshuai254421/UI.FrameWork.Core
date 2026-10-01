using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Infrastructure
{
    /// <summary>
    /// 日志模块数据库上下文：默认连接 SQLite 日志库，自动应用本程序集内的实体配置。
    /// </summary>
    public partial class DataContext : DbContext
    {
        /// <summary>通过上下文选项构造，便于测试替换为内存库。</summary>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>配置数据库连接：未显式传入选项时回退到默认 SQLite 日志库；调试模式额外输出 SQL 与敏感数据日志。</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
#endif
         // 仅当未显式传入选项（如测试传入内存库）时才落到磁盘库。
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(AppGlobals.LogDFliePath);
            }

            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>应用本程序集内全部实体类型配置。</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
