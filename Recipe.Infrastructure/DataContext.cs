using EFCore.Infrastructure;
using EFCore.Repository;
using Framework.Core.Common;
using Framework.Core.CustomAttribute;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Recipe.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 设计时 DbContext 工厂：供 dotnet-ef 等设计时工具创建 DataContext。
    /// </summary>
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        #region IDesignTimeDbContextFactory

        /// <summary>创建设计时 DataContext：固定使用 SQLite 与全局配方库连接字符串。</summary>
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

    /// <summary>
    /// 配方系统数据库上下文：聚合配方、配方管理器、相机配置等实体，支持插件实体扩展与默认数据播种。
    /// </summary>
    public partial class DataContext : DbContext
    {
        #region 构造函数

        /// <summary>构造函数：传入 DbContext 选项（生产为 SQLite 文件库，测试可为内存库）。</summary>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        #endregion

        #region 方法

        /// <summary>配置数据库选项：调试时按开关输出 SQL 与敏感数据日志（#30：EF_SQL_LOG=1 才开，四家族同款）；
        /// 未显式配置（如测试传内存库）时回退到全局 SQLite 配方库。</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.UseDiagnosticSqlLogging();   // #30：EF_SQL_LOG=1 才开，四家族同款一句门
#endif

            // 仅当未显式传入选项（如测试传入内存库）时才落到磁盘库。
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(AppGlobals.RecipeDbFliePath);
            }

            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>构建模型：应用本程序集与插件程序集的实体配置，兜底注册派生实体，并播种默认配方、管理器与相机配置。</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. 扫描当前程序集里声明归属本 DataContext 的插件
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            // 2. 【新增】扩展点：扫描其它程序集里声明归属本 DataContext 的插件
            foreach (var assembly in GetExtensionAssemblies())
            {
                // 应用插件自带的 IEntityTypeConfiguration（表名、索引、HasData 都在插件里配）
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }


            // 3. 【新增】兜底：插件即使没写任何配置，派生实体也自动进模型
            foreach (var type in FindRecipeParameterDerivedTypes())
            {
                modelBuilder.Entity(type);
            }

            foreach(var type in FindSystemParameterDerivedTypes())
            {
                modelBuilder.Entity(type);
            }

            modelBuilder.Entity<global::Recipe.Domain.Recipe>().HasData(
                new global::Recipe.Domain.Recipe(-1, "default", "-Default-", "ZS1A")
            );
            modelBuilder.Entity<RecipeManager>().HasData(
                new RecipeManager(1, -1)  // 指向默认配方
            );

            modelBuilder.Entity<CameraConfiguration>().HasData(
                new CameraConfiguration(1,-1, "CameraA"),
                 new CameraConfiguration(2,-1, "CameraB"),
                  new CameraConfiguration(3,-1, "CameraC"),
                   new CameraConfiguration(4,-1, "CameraD")
            );

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// 找出所有程序集里 RecipeParameter 的非抽象派生类
        /// </summary>
        private static IEnumerable<Type> FindRecipeParameterDerivedTypes()
        {
            foreach (var asm in GetExtensionAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 插件引用了缺失依赖时 GetTypes 会抛这个，必须吞掉继续
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }

                foreach (var t in types)
                {
                    if (t.IsClass && !t.IsAbstract &&
                        t != typeof(RecipeParameter) && typeof(RecipeParameter).IsAssignableFrom(t))
                    {
                        yield return t;
                    }
                }
            }
        }

        /// <summary>
        /// 找出所有程序集里 RecipeParameter 的非抽象派生类
        /// </summary>
        private static IEnumerable<Type> FindSystemParameterDerivedTypes()
        {
            foreach (var asm in GetExtensionAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 插件引用了缺失依赖时 GetTypes 会抛这个，必须吞掉继续
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }

                foreach (var t in types)
                {
                    if (t.IsClass && !t.IsAbstract &&
                        t != typeof(Entity) && typeof(Entity).IsAssignableFrom(t))
                    {
                        yield return t;
                    }
                }
            }
        }



        /// <summary>
        /// 只返回“声明默认 DbContext 是 Recipe.DataContext”的外部程序集
        /// </summary>
        private static IEnumerable<Assembly> GetExtensionAssemblies()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic || asm == typeof(DataContext).Assembly) continue;

                var attr = asm.GetCustomAttribute<DefaultDbContextAttribute>();
                if (attr?.DbContextType == typeof(DataContext))
                {
                    yield return asm;
                }
            }
        }

        #endregion
    }
}
