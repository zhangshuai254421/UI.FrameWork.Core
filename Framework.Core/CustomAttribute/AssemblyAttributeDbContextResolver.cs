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

                // typeof 形态直接用；字符串形态（实体程序集禁止引用 DbContext 程序集时）按名解析
                if (attribute.DbContextType != null)
                {
                    return attribute.DbContextType;
                }

                var resolved = Type.GetType(attribute.DbContextTypeName, throwOnError: false);

                if (resolved == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve DbContext '{attribute.DbContextTypeName}' declared by [DefaultDbContext] " +
                        $"on assembly '{assembly.GetName().Name}'. 请确认目标程序集会被加载（被任一已加载工程引用）。");
                }

                return resolved;
            });

            // 从 DI 容器中获取实例
            return (DbContext)_serviceProvider.GetRequiredService(contextType);
        }
    }
}
