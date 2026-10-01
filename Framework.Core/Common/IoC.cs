using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.Common
{
    /// <summary>
    /// 轻量级静态 IoC 入口：由组合根在启动时注入解析与注入委托；未初始化时调用会抛出异常。
    /// </summary>
    public static class IoC
    {
        /// <summary>按类型与可选键解析实例的委托；未初始化时抛出异常。</summary>
        public static Func<Type, string, object> GetInstance = (service, key) => { throw new InvalidOperationException("IoC is not initialized"); };

     
        /// <summary>对已创建对象执行属性注入的委托；未初始化时抛出异常。</summary>
        public static Action<object> BuildUp = instance => { throw new InvalidOperationException("IoC is not initialized"); };

        /// <summary>按类型（可选键）解析实例并强转为 T；未初始化时抛出异常。</summary>
        public static T Get<T>(string key = null)
        {
            return (T)GetInstance(typeof(T), key);
        }

  
    }
}
