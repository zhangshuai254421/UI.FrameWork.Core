using Microsoft.EntityFrameworkCore;
using System;

namespace Framework.Core.CustomAttribute
{
    // 定义特性，用于标记程序集默认使用的 DbContext
    // 特性必须跟实体同程序集（AssemblyAttributeDbContextResolver 按实体所在程序集反查）
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class DefaultDbContextAttribute : Attribute
    {
        /// <summary>
        /// typeof 形态：实体与 DbContext 同程序集（如 Device 家族），或实体程序集可以合法引用 DbContext 程序集（如插件程序集）时使用。
        /// </summary>
        public Type DbContextType { get; }

        /// <summary>
        /// 字符串形态：实体程序集不能引用 DbContext 程序集时使用（如 Domain → Infrastructure 会被分层禁掉），写程序集全名 "命名空间.类型名, 程序集名"。
        /// </summary>
        public string DbContextTypeName { get; }

        public DefaultDbContextAttribute(Type dbContextType)
        {
            if (!dbContextType.IsSubclassOf(typeof(DbContext)))
                throw new ArgumentException($"{dbContextType.Name} must inherit from DbContext");

            DbContextType = dbContextType;
        }

        public DefaultDbContextAttribute(string dbContextTypeName)
        {
            DbContextTypeName = dbContextTypeName;
        }
    }
}
