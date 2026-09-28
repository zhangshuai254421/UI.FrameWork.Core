using System.Reflection;

namespace Framework.Device
{
    /// <summary>
    /// 适配器工厂的生产实现：发现带 <see cref="DeviceAdapterAttribute"/> 的适配器类，
    /// 建立「(设备类型, 厂商) → 适配器类型」的注册映射，按需实例化。
    /// 厂商适配器为独立程序集，通过 <see cref="FromDirectory"/> 在启动时动态加载，不硬引用具体厂商。
    /// 约定：适配器须暴露无参构造函数，供工厂反射实例化（不注入依赖）。
    /// </summary>
    public class DeviceAdapterFactory : IDeviceAdapterFactory
    {
        private readonly Dictionary<(DeviceKind Kind, string Vendor), Type> _adapterTypes = new();

        /// <summary>从给定适配器类型建立注册映射（测试或显式注册用，不触发真实 DLL 加载）。</summary>
        public DeviceAdapterFactory(IEnumerable<Type> adapterTypes)
        {
            ArgumentNullException.ThrowIfNull(adapterTypes);
            foreach (var type in adapterTypes)
            {
                Register(type);
            }
        }

        /// <summary>从给定程序集发现适配器类型并建立注册映射。</summary>
        public DeviceAdapterFactory(IEnumerable<Assembly> assemblies)
            : this(DiscoverTypes(assemblies))
        {
        }

        /// <summary>
        /// 扫描目录下的程序集并建立注册映射；加载失败的非插件程序集被跳过，不中断启动。
        /// 新增一个厂商 = 放一个适配器 DLL + 一条配置记录，无需重编译主程序。
        /// </summary>
        public static DeviceAdapterFactory FromDirectory(string directory, string pattern = "*.dll")
        {
            var types = new List<Type>();
            foreach (var file in Directory.GetFiles(directory, pattern))
            {
                try
                {
                    types.AddRange(DiscoverTypes(new[] { Assembly.LoadFrom(file) }));
                }
                catch (Exception)
                {
                    // 非插件程序集或加载失败，跳过。
                }
            }

            return new DeviceAdapterFactory(types);
        }

        /// <inheritdoc />
        public IDevice Create(DeviceKind kind, string vendor)
        {
            if (!_adapterTypes.TryGetValue((kind, vendor), out var type))
            {
                throw new InvalidOperationException($"未注册适配器 ({kind}, {vendor})");
            }

            return (IDevice)Activator.CreateInstance(type)!;
        }

        /// <summary>登记一个适配器类型；无标记、抽象类型或未实现 IDevice 的类型忽略，同 (类型, 厂商) 键后注册覆盖先注册。</summary>
        private void Register(Type type)
        {
            var attribute = type.GetCustomAttribute<DeviceAdapterAttribute>();
            if (attribute is null || type.IsAbstract || type.IsInterface)
            {
                return;
            }

            if (!typeof(IDevice).IsAssignableFrom(type))
            {
                return;
            }

            _adapterTypes[(attribute.Kind, attribute.Vendor)] = type;
        }

        private static IEnumerable<Type> DiscoverTypes(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 部分类型依赖缺失（如厂商 SDK）时，仅保留已加载成功的类型。
                    types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
                }

                foreach (var type in types)
                {
                    yield return type;
                }
            }
        }
    }
}
