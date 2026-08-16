using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.Common
{
    // Framework.Core/Common/AssemblyPreloader.cs （新增）
    public static class AssemblyPreloader
    {
        // 不用逐个黑名单，直接“只加载声明了 DefaultDbContext 特性的”更精准
        public static void PreloadPluginAssemblies(string directory, string pattern = "*.dll")
        {
            foreach (var file in Directory.GetFiles(directory, pattern))
            {
                try
                {
                    var asm = Assembly.LoadFrom(file);
                    // 顺手验证一下是不是数据插件，非插件 dll 加载了也无害
                }
                catch (Exception ex)
                {
                    // 记录日志但绝不中断启动
                }
            }
        }
    }
}
