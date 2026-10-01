using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SemiControl.Data

{
    /// <summary>
    ///     装箱后的值类型（用于提高效率）
    /// </summary>
    internal static class ValueBoxes
    {
        /// <summary>装箱后的 true。</summary>
        internal static object TrueBox = true;

        /// <summary>装箱后的 false。</summary>
        internal static object FalseBox = false;

        /// <summary>装箱后的 Orientation.Vertical。</summary>
        internal static object VerticalBox = Orientation.Vertical;

        /// <summary>装箱后的 Orientation.Horizontal。</summary>
        internal static object HorizontalBox = Orientation.Horizontal;

        /// <summary>装箱后的 Visibility.Visible。</summary>
        internal static object VisibleBox = Visibility.Visible;

        /// <summary>装箱后的 Visibility.Collapsed。</summary>
        internal static object CollapsedBox = Visibility.Collapsed;

        /// <summary>装箱后的 Visibility.Hidden。</summary>
        internal static object HiddenBox = Visibility.Hidden;

        /// <summary>装箱后的 0.1。</summary>
        internal static object Double01Box = .1;

        /// <summary>装箱后的 0.0。</summary>
        internal static object Double0Box = .0;

        /// <summary>装箱后的 1.0。</summary>
        internal static object Double1Box = 1.0;

        /// <summary>装箱后的 10.0。</summary>
        internal static object Double10Box = 10.0;

        /// <summary>装箱后的 20.0。</summary>
        internal static object Double20Box = 20.0;

        /// <summary>装箱后的 100.0。</summary>
        internal static object Double100Box = 100.0;

        /// <summary>装箱后的 200.0。</summary>
        internal static object Double200Box = 200.0;

        /// <summary>装箱后的 300.0。</summary>
        internal static object Double300Box = 300.0;

        /// <summary>装箱后的 -1.0。</summary>
        internal static object DoubleNeg1Box = -1.0;

        /// <summary>装箱后的 0。</summary>
        internal static object Int0Box = 0;

        /// <summary>装箱后的 1。</summary>
        internal static object Int1Box = 1;

        /// <summary>装箱后的 2。</summary>
        internal static object Int2Box = 2;

        /// <summary>装箱后的 5。</summary>
        internal static object Int5Box = 5;

        /// <summary>装箱后的 99。</summary>
        internal static object Int99Box = 99;

        /// <summary>取布尔值的装箱对象（复用缓存，避免重复装箱）。</summary>
        internal static object BooleanBox(bool value) => value ? TrueBox : FalseBox;

        /// <summary>取方向的装箱对象（复用缓存，避免重复装箱）。</summary>
        internal static object OrientationBox(Orientation value) =>
            value == Orientation.Horizontal ? HorizontalBox : VerticalBox;

        /// <summary>取可见性的装箱对象；非法值抛参数越界异常。</summary>
        internal static object VisibilityBox(Visibility value)
        {
            return value switch
            {
                Visibility.Visible => VisibleBox,
                Visibility.Hidden => HiddenBox,
                Visibility.Collapsed => CollapsedBox,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }
}
