using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SemiControl.Controls
{
    /// <summary>
    /// 按钮附加属性
    /// </summary>
    public static class ButtonAttach
    {
        /// <summary>
        /// 设置图标属性
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Geometry GetIconGeometory(DependencyObject obj)
        {
            return (Geometry)obj.GetValue(IconGeometoryProperty);
        }

        /// <summary>
        /// 获得图标属性
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetIconGeometory(DependencyObject obj, Geometry value)
        {
            obj.SetValue(IconGeometoryProperty, value);
        }
        /// <summary>
        /// 图标属性
        /// </summary>
        public static readonly DependencyProperty IconGeometoryProperty = DependencyProperty.RegisterAttached("IconGeometory", typeof(Geometry), typeof(ButtonAttach), new PropertyMetadata(null));
    }
}
