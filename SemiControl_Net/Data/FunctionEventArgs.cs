using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SemiControl.Data
{
    /// <summary>
    /// 携带业务数据的路由事件参数：除标准路由信息外，通过 <see cref="Info"/> 传递附加数据。
    /// </summary>
    /// <typeparam name="T">附加数据的类型。</typeparam>
    public class FunctionEventArgs<T> : RoutedEventArgs
    {
        /// <summary>使用附加数据构造参数（用于直接触发事件）。</summary>
        public FunctionEventArgs(T info)
        {
            Info = info;
        }

        /// <summary>使用路由事件与事件源构造参数（用于引发路由事件）。</summary>
        public FunctionEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
        }

        /// <summary>随事件传递的附加数据。</summary>
        public T Info { get; set; }
    }
}
