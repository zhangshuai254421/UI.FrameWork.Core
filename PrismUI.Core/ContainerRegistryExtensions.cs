using System.Reflection;
using Prism.Ioc;

namespace PrismUI.Core;

public static class ContainerRegistryExtensions
{
    /// <summary>
    /// 批量注册程序集中所有以 "View" 结尾的 UserControl 为导航视图
    /// 等价于对每个 View 调用 RegisterForNavigation&lt;TView&gt;()
    /// </summary>
    public static IContainerRegistry RegisterViewsFromAssembly(this IContainerRegistry containerRegistry, Assembly assembly)
    {
        var viewTypes = assembly.GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && t.Name.EndsWith("View")
                && t.BaseType?.Name == "UserControl");

        foreach (var viewType in viewTypes)
        {
            // RegisterForNavigation<TView> 内部就是 Register(typeof(object), viewType, viewType.Name)
            containerRegistry.Register(typeof(object), viewType, viewType.Name);
        }

        return containerRegistry;
    }

    /// <summary>
    /// 批量注册指定类型所在程序集中所有 View
    /// </summary>
    public static IContainerRegistry RegisterViewsFromAssembly<TAssemblyMarker>(this IContainerRegistry containerRegistry)
    {
        return containerRegistry.RegisterViewsFromAssembly(typeof(TAssemblyMarker).Assembly);
    }
}
