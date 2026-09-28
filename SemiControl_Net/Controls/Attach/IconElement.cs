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
    /// 图标附加属性
    /// </summary>
    public class IconElement
    {
        /// <summary>
        /// 获得图标几何图形
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>图标几何图形</returns>
        public static Geometry GetGeometry(DependencyObject element) => (Geometry)element.GetValue(GeometryProperty);

        /// <summary>
        /// 设置图标几何图形
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="value">图标几何图形</param>
        public static void SetGeometry(DependencyObject element, Geometry value) => element.SetValue(GeometryProperty, value);

        /// <summary>
        /// 图标几何图形
        /// </summary>
        public static readonly DependencyProperty GeometryProperty = DependencyProperty.RegisterAttached("Geometry", typeof(Geometry), typeof(IconElement), new PropertyMetadata(default(Geometry)));

        /// <summary>
        /// 获得图像资源
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>图像资源</returns>
        public static ImageSource GetSource(DependencyObject element) => (ImageSource)element.GetValue(GeometryProperty);

        /// <summary>
        /// 设置图像资源
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="value">图像资源</param>
        public static void SetSource(DependencyObject element, ImageSource value) => element.SetValue(GeometryProperty, value);

        /// <summary>
        /// 图像资源
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached("Source", typeof(ImageSource), typeof(IconElement), new PropertyMetadata(default(ImageSource)));

        /// <summary>
        /// 获得宽度
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>宽度</returns>
        public static double GetWidth(DependencyObject element) => (double)element.GetValue(WidthProperty);

        /// <summary>
        /// 设置宽度
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="value">宽度</param>
        public static void SetWidth(DependencyObject element, double value) => element.SetValue(WidthProperty, value);

        /// <summary>
        /// 宽度
        /// </summary>
        public static readonly DependencyProperty WidthProperty = DependencyProperty.RegisterAttached("Width", typeof(double), typeof(IconElement), new PropertyMetadata(double.NaN));

        /// <summary>
        /// 获得高度
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>高度</returns>
        public static double GetHeight(DependencyObject element) => (double)element.GetValue(HeightProperty);

        /// <summary>
        /// 高度
        /// </summary>
        public static readonly DependencyProperty HeightProperty = DependencyProperty.RegisterAttached("Height", typeof(double), typeof(IconElement), new PropertyMetadata(double.NaN));

        /// <summary>
        /// 设置高度
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="value">高度</param>
        public static void SetHeight(DependencyObject element, double value) => element.SetValue(HeightProperty, value);
    }
}
