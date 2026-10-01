using EFCore.Repository;
using Framework.Core.CustomAttribute;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Infrastructure
{
    /// <summary>
    /// 数据访问层依赖注入扩展：注册仓储、工作单元与 DbContext 解析器，并自动发现注册 DbContext。
    /// </summary>
    public static class DalExtensions
    {
        /// <summary>注册泛型仓储、工作单元与 DbContext 解析器，并自动注册程序集中所有具体 DbContext。</summary>
        public static IServiceCollection AddRepository(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // 2. 注册解析器（单例，因为是无状态的）
            services.AddSingleton<IDbContextResolver, AssemblyAttributeDbContextResolver>();

            // 4. 可选：自动发现并注册所有 DbContext（避免手动添加）
            AutoRegisterDbContexts(services);
            return services;
        }
        /// <summary>扫描已加载的全部程序集，把其中所有具体 DbContext 类型以 Scoped 注册进容器。</summary>
        private static void AutoRegisterDbContexts(IServiceCollection services)
        {
            var dbContextTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(DbContext )));

            foreach (var contextType in dbContextTypes)
            {
                services.AddScoped(contextType);
            }
        }

        /// <summary>确保指定 DbContext 的数据库与表已创建；数据库已存在时不执行任何操作，返回原服务提供程序。</summary>
        public static IServiceProvider UseDatabaseEnsureCreated<TDbContext>(this IServiceProvider provider)
            where TDbContext : DbContext
        {
            using var serviceScope = provider.CreateScope();

            var serviceProvider = serviceScope.ServiceProvider;

            // EnsureCreated 方法会创建数据库和所有表，如果数据库已存在，则不会执行任何操作
            var context = serviceProvider.GetRequiredService<TDbContext>();
            context.Database.EnsureCreated();

            // EnsureCreated 换成 CreateTables：只创建模型中存在、库里缺失的表
            //var creator = context.GetService<IRelationalDatabaseCreator>();
            //creator!.CreateTables();

            return provider;
        }
    }
}
