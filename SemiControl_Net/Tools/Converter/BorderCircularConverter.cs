using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SemiControl.Tools
{
    /// <summary>
    /// 圆形边框转换器：把元素的宽、高（MultiBinding 两路输入）换算成正圆的 <see cref="CornerRadius"/>（短边的一半）。
    /// 配合 <see cref="Controls.BorderElement.CircularProperty"/> 附加属性使用，实现 Border 随尺寸变化的圆形裁剪。
    /// </summary>
    public class BorderCircularConverter : IMultiValueConverter
    {
        /// <summary>由 [实际宽度, 实际高度] 计算圆形圆角；输入缺失返回 UnsetValue，尺寸非正返回 0 圆角。</summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not double width || values[1] is not double height)
            {
                return DependencyProperty.UnsetValue;
            }

            if (width < double.Epsilon || height < double.Epsilon)
            {
                return new CornerRadius();
            }

            var min = Math.Min(width, height);
            return new CornerRadius(min / 2);
        }

        /// <summary>不支持反向转换。</summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
