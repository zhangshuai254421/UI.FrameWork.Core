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
        /// 获得图标属性
        /// </summary>
        /// <param name="obj">目标元素</param>
        /// <returns>图标几何图形</returns>
        public static Geometry GetIconGeometory(DependencyObject obj)
        {
            return (Geometry)obj.GetValue(IconGeometoryProperty);
        }

        /// <summary>
        /// 设置图标属性
        /// </summary>
        /// <param name="obj">目标元素</param>
        /// <param name="value">图标几何图形</param>
        public static void SetIconGeometory(DependencyObject obj, Geometry value)
        {
            obj.SetValue(IconGeometoryProperty, value);
        }
        /// <summary>
        /// 图标属性（属性名 Geometory 为历史拼写笔误，因已对外发布、XAML 中广泛使用而保留，请勿改名）
        /// </summary>
        public static readonly DependencyProperty IconGeometoryProperty = DependencyProperty.RegisterAttached("IconGeometory", typeof(Geometry), typeof(ButtonAttach), new PropertyMetadata(null));
    }
}
