using SemiControl.Data;
using System.Windows;
using System.Windows.Media;

namespace SemiControl.Controls.Attach
{
    /// <summary>
    /// 标题放置位置
    /// </summary>
    public enum TitlePlacementType
    {
        /// <summary>标题在左。</summary>
        Left,

        /// <summary>标题在上。</summary>
        Top
    }

    /// <summary>
    /// 标题附加属性：为带表单标题的控件（如 GroupBox 类布局）统一提供标题文本、
    /// 颜色、放置方向、宽度及间距等附加设置，多数属性值可沿可视树继承。
    /// </summary>
    public class TitleElement
    {
        /// <summary>Title 附加属性：标题文本。</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(
            "Title", typeof(string), typeof(TitleElement), new PropertyMetadata(default(string)));

        /// <summary>设置标题文本。</summary>
        public static void SetTitle(DependencyObject element, string value)
            => element.SetValue(TitleProperty, value);

        /// <summary>获取标题文本。</summary>
        public static string GetTitle(DependencyObject element)
            => (string)element.GetValue(TitleProperty);

        /// <summary>Background 附加属性：标题区背景色（可继承）。</summary>
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.RegisterAttached(
            "Background", typeof(Brush), typeof(TitleElement), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题区背景色。</summary>
        public static void SetBackground(DependencyObject element, Brush value)
            => element.SetValue(BackgroundProperty, value);

        /// <summary>获取标题区背景色。</summary>
        public static Brush GetBackground(DependencyObject element)
            => (Brush)element.GetValue(BackgroundProperty);

        /// <summary>Foreground 附加属性：标题前景色/文字颜色（可继承）。</summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.RegisterAttached(
            "Foreground", typeof(Brush), typeof(TitleElement), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题前景色。</summary>
        public static void SetForeground(DependencyObject element, Brush value)
            => element.SetValue(ForegroundProperty, value);

        /// <summary>获取标题前景色。</summary>
        public static Brush GetForeground(DependencyObject element)
            => (Brush)element.GetValue(ForegroundProperty);

        /// <summary>BorderBrush 附加属性：标题区边框色（可继承）。</summary>
        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.RegisterAttached(
            "BorderBrush", typeof(Brush), typeof(TitleElement), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题区边框色。</summary>
        public static void SetBorderBrush(DependencyObject element, Brush value)
            => element.SetValue(BorderBrushProperty, value);

        /// <summary>获取标题区边框色。</summary>
        public static Brush GetBorderBrush(DependencyObject element)
            => (Brush)element.GetValue(BorderBrushProperty);

        /// <summary>TitlePlacement 附加属性：标题放置位置（左/上，默认在上，可继承）。</summary>
        public static readonly DependencyProperty TitlePlacementProperty = DependencyProperty.RegisterAttached(
            "TitlePlacement", typeof(TitlePlacementType), typeof(TitleElement), new FrameworkPropertyMetadata(TitlePlacementType.Top, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题放置位置。</summary>
        public static void SetTitlePlacement(DependencyObject element, TitlePlacementType value)
            => element.SetValue(TitlePlacementProperty, value);

        /// <summary>获取标题放置位置。</summary>
        public static TitlePlacementType GetTitlePlacement(DependencyObject element)
            => (TitlePlacementType)element.GetValue(TitlePlacementProperty);

        /// <summary>TitleWidth 附加属性：标题列宽度（GridLength，默认 Auto，可继承）。</summary>
        public static readonly DependencyProperty TitleWidthProperty = DependencyProperty.RegisterAttached(
            "TitleWidth", typeof(GridLength), typeof(TitleElement), new FrameworkPropertyMetadata(GridLength.Auto, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题列宽度。</summary>
        public static void SetTitleWidth(DependencyObject element, GridLength value) => element.SetValue(TitleWidthProperty, value);

        /// <summary>获取标题列宽度。</summary>
        public static GridLength GetTitleWidth(DependencyObject element) => (GridLength)element.GetValue(TitleWidthProperty);

        /// <summary>HorizontalAlignment 附加属性：标题水平对齐（可继承）。</summary>
        public static readonly DependencyProperty HorizontalAlignmentProperty = DependencyProperty.RegisterAttached(
            "HorizontalAlignment", typeof(HorizontalAlignment), typeof(TitleElement), new FrameworkPropertyMetadata(default(HorizontalAlignment), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题水平对齐。</summary>
        public static void SetHorizontalAlignment(DependencyObject element, HorizontalAlignment value)
            => element.SetValue(HorizontalAlignmentProperty, value);

        /// <summary>获取标题水平对齐。</summary>
        public static HorizontalAlignment GetHorizontalAlignment(DependencyObject element)
            => (HorizontalAlignment)element.GetValue(HorizontalAlignmentProperty);

        /// <summary>VerticalAlignment 附加属性：标题垂直对齐（可继承）。</summary>
        public static readonly DependencyProperty VerticalAlignmentProperty = DependencyProperty.RegisterAttached(
            "VerticalAlignment", typeof(VerticalAlignment), typeof(TitleElement), new FrameworkPropertyMetadata(default(VerticalAlignment), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题垂直对齐。</summary>
        public static void SetVerticalAlignment(DependencyObject element, VerticalAlignment value)
            => element.SetValue(VerticalAlignmentProperty, value);

        /// <summary>获取标题垂直对齐。</summary>
        public static VerticalAlignment GetVerticalAlignment(DependencyObject element)
            => (VerticalAlignment)element.GetValue(VerticalAlignmentProperty);

        /// <summary>MarginOnTheLeft 附加属性：标题在左时的外边距（可继承）。</summary>
        public static readonly DependencyProperty MarginOnTheLeftProperty = DependencyProperty.RegisterAttached(
            "MarginOnTheLeft", typeof(Thickness), typeof(TitleElement), new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题在左时的外边距。</summary>
        public static void SetMarginOnTheLeft(DependencyObject element, Thickness value)
            => element.SetValue(MarginOnTheLeftProperty, value);

        /// <summary>获取标题在左时的外边距。</summary>
        public static Thickness GetMarginOnTheLeft(DependencyObject element)
            => (Thickness)element.GetValue(MarginOnTheLeftProperty);

        /// <summary>MarginOnTheTop 附加属性：标题在上时的外边距（可继承）。</summary>
        public static readonly DependencyProperty MarginOnTheTopProperty = DependencyProperty.RegisterAttached(
            "MarginOnTheTop", typeof(Thickness), typeof(TitleElement), new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题在上时的外边距。</summary>
        public static void SetMarginOnTheTop(DependencyObject element, Thickness value)
            => element.SetValue(MarginOnTheTopProperty, value);

        /// <summary>获取标题在上时的外边距。</summary>
        public static Thickness GetMarginOnTheTop(DependencyObject element)
            => (Thickness)element.GetValue(MarginOnTheTopProperty);

        /// <summary>Padding 附加属性：标题区内边距（可继承）。</summary>
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.RegisterAttached(
            "Padding", typeof(Thickness), typeof(TitleElement), new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>设置标题区内边距。</summary>
        public static void SetPadding(DependencyObject element, Thickness value) => element.SetValue(PaddingProperty, value);

        /// <summary>获取标题区内边距。</summary>
        public static Thickness GetPadding(DependencyObject element) => (Thickness)element.GetValue(PaddingProperty);

        /// <summary>MinHeight 附加属性：最小高度。</summary>
        public static readonly DependencyProperty MinHeightProperty =
            DependencyProperty.RegisterAttached("MinHeight", typeof(double), typeof(TitleElement), new PropertyMetadata(ValueBoxes.Double0Box));

        /// <summary>获取最小高度。</summary>
        public static double GetMinHeight(DependencyObject obj) => (double)obj.GetValue(MinHeightProperty);

        /// <summary>设置最小高度。</summary>
        public static void SetMinHeight(DependencyObject obj, double value) => obj.SetValue(MinHeightProperty, value);

        /// <summary>MinWidth 附加属性：最小宽度。</summary>
        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.RegisterAttached("MinWidth", typeof(double), typeof(TitleElement), new PropertyMetadata(ValueBoxes.Double0Box));

        /// <summary>获取最小宽度。</summary>
        public static double GetMinWidth(DependencyObject obj) => (double)obj.GetValue(MinWidthProperty);

        /// <summary>设置最小宽度。</summary>
        public static void SetMinWidth(DependencyObject obj, double value) => obj.SetValue(MinWidthProperty, value);
    }
}
