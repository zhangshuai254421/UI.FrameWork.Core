using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SemiControl.Controls
{
    /// <summary>
    /// 切换按钮
    /// </summary>
    public class ToggleIconButton : ToggleButton
    {
        /// <summary>
        /// 图标几何图形
        /// </summary>
        public Geometry IconGeometry
        {
            get { return (Geometry)GetValue(IconGeometryProperty); }
            set { SetValue(IconGeometryProperty, value); }
        }

        /// <summary>
        /// 图标几何图形
        /// </summary>
        public static readonly DependencyProperty IconGeometryProperty = DependencyProperty.Register("IconGeometry", typeof(Geometry), typeof(ToggleIconButton), new PropertyMetadata(null));

        /// <summary>
        /// 图标高度
        /// </summary>
        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        /// <summary>
        /// 图标高度
        /// </summary>
        public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register("IconHeight", typeof(double), typeof(ToggleIconButton), new PropertyMetadata((double)20));

        /// <summary>
        /// 图标宽度
        /// </summary>
        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        /// <summary>
        /// 图标宽度
        /// </summary>
        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register("IconWidth", typeof(double), typeof(ToggleIconButton), new PropertyMetadata((double)20));

        /// <summary>
        /// 左上角字体或颜色对象
        /// </summary>
        public object TopLeftContent
        {
            get { return (object)GetValue(TopLeftContentProperty); }
            set { SetValue(TopLeftContentProperty, value); }
        }

        /// <summary>
        /// 左上角字体或颜色对象
        /// </summary>
        public static readonly DependencyProperty TopLeftContentProperty = DependencyProperty.Register("TopLeftContent", typeof(object), typeof(ToggleIconButton), new PropertyMetadata(null));

        /// <summary>
        /// 状态颜色
        /// </summary>
        public Brush StatusBrush
        {
            get { return (Brush)GetValue(StatusBrushProperty); }
            set { SetValue(StatusBrushProperty, value); }
        }

        /// <summary>
        /// 状态颜色
        /// </summary>
        public static readonly DependencyProperty StatusBrushProperty = DependencyProperty.Register("StatusBrush", typeof(Brush), typeof(ToggleIconButton), new PropertyMetadata(Brushes.White));

        /// <summary>
        /// 是否显示状态 小矩形框
        /// </summary>
        public bool IsShowStatus
        {
            get { return (bool)GetValue(IsShowStatusProperty); }
            set { SetValue(IsShowStatusProperty, value); }
        }

        /// <summary>
        /// 是否显示状态
        /// </summary>
        public static readonly DependencyProperty IsShowStatusProperty = DependencyProperty.Register("IsShowStatus", typeof(bool), typeof(ToggleIconButton), new PropertyMetadata(false));
    }
}
