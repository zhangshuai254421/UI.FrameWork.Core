using EFCore.Repository;
using Framework.Core.CustomAttribute;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Infrastructure
{
    public static class DalExtensions
    {
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

        public static IServiceProvider UseDatabaseEnsureCreated<TDbContext>(this IServiceProvider provider)
            where TDbContext : DbContext
        {
            using var serviceScope = provider.CreateScope();

            var serviceProvider = serviceScope.ServiceProvider;

            var context = serviceProvider.GetRequiredService<TDbContext>();

            context.Database.EnsureCreated();

            return provider;
        }
    }
}
