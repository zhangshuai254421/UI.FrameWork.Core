using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace Framework.Core.CustomAttribute
{
    // 基于程序集特性的解析器实现
    public class AssemblyAttributeDbContextResolver : IDbContextResolver
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Type, Type> _entityContextCache = new();

        public AssemblyAttributeDbContextResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public DbContext Resolve<TEntity>() where TEntity : class
        {
            var entityType = typeof(TEntity);

            // 从缓存中获取或解析 Context 类型
            var contextType = _entityContextCache.GetOrAdd(entityType, type =>
            {
                var assembly = type.Assembly;
                var attribute = assembly.GetCustomAttribute<DefaultDbContextAttribute>();

                if (attribute == null)
                {
                    throw new InvalidOperationException(
                        $"Assembly '{assembly.FullName}' does not have DefaultDbContextAttribute. " +
                        $"Please add [assembly: DefaultDbContext(typeof(YourDbContext))] to the assembly.");
                }

                return attribute.DbContextType;
            });

            // 从 DI 容器中获取实例
            return (DbContext)_serviceProvider.GetRequiredService(contextType);
        }
    }
}
