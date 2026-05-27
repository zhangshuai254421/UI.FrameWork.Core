using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.CustomAttribute
{
    // 定义特性，用于标记程序集默认使用的 DbContext
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class DefaultDbContextAttribute : Attribute
    {
        public Type DbContextType { get; }

        public DefaultDbContextAttribute(Type dbContextType)
        {
            if (!dbContextType.IsSubclassOf(typeof(DbContext)))
                throw new ArgumentException($"{dbContextType.Name} must inherit from DbContext");

            DbContextType = dbContextType;
        }
    }
}
