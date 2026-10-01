using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace Framework.Core.CustomAttribute
{
    // 基于程序集特性的解析器实现
    public class AssemblyAttributeDbContextResolver : IDbContextResolver
    {
        /// <summary>用于从 DI 容器取出 DbContext 实例。</summary>
        private readonly IServiceProvider _serviceProvider;
        /// <summary>实体类型到 DbContext 类型的缓存，避免重复反射。</summary>
        private readonly ConcurrentDictionary<Type, Type> _entityContextCache = new();

        /// <summary>保存服务提供程序供后续解析使用。</summary>
        public AssemblyAttributeDbContextResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>读取实体所在程序集的 DefaultDbContext 特性并从容器解析对应上下文；未声明特性或类型无法解析时抛出异常。</summary>
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
