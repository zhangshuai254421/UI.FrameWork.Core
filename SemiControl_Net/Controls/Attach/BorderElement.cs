using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SemiControl.Data;
using SemiControl.Tools;

namespace SemiControl.Controls
{
    /// <summary>
    /// 边框附加属性：为任意 <see cref="Border"/>（或模板宿主）提供统一圆角与圆形裁剪能力。
    /// </summary>
    public class BorderElement
    {
        /// <summary>CornerRadius 附加属性：边框圆角（值可继承，样式与模板经它取宿主圆角）。</summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
            "CornerRadius", typeof(CornerRadius), typeof(BorderElement), new FrameworkPropertyMetadata(default(CornerRadius), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置边框圆角。</summary>
        public static void SetCornerRadius(DependencyObject element, CornerRadius value) => element.SetValue(CornerRadiusProperty, value);

        /// <summary>获取边框圆角。</summary>
        public static CornerRadius GetCornerRadius(DependencyObject element) => (CornerRadius)element.GetValue(CornerRadiusProperty);

        /// <summary>
        /// Circular 附加属性：true 时 Border 裁剪为正圆 ——
        /// 监听 ActualWidth/ActualHeight 绑定 <see cref="BorderCircularConverter"/> 动态计算圆角；置 false 解除绑定。
        /// </summary>
        public static readonly DependencyProperty CircularProperty = DependencyProperty.RegisterAttached(
            "Circular", typeof(bool), typeof(BorderElement), new PropertyMetadata(ValueBoxes.FalseBox, OnCircularChanged));

        private static void OnCircularChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Border border)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                var binding = new MultiBinding
                {
                    Converter = new BorderCircularConverter()
                };
                binding.Bindings.Add(new Binding(FrameworkElement.ActualWidthProperty.Name) { Source = border });
                binding.Bindings.Add(new Binding(FrameworkElement.ActualHeightProperty.Name) { Source = border });
                border.SetBinding(Border.CornerRadiusProperty, binding);
            }
            else
            {
                BindingOperations.ClearBinding(border, Border.CornerRadiusProperty);
            }
        }

        /// <summary>设置是否圆形裁剪。</summary>
        public static void SetCircular(DependencyObject element, bool value)
            => element.SetValue(CircularProperty, ValueBoxes.BooleanBox(value));

        /// <summary>获取是否圆形裁剪。</summary>
        public static bool GetCircular(DependencyObject element)
            => (bool)element.GetValue(CircularProperty);
    }
}
