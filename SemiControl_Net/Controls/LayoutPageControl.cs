// Controls/LayoutPageControl.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SemiControl.Controls
{
/// <summary>
/// 页面布局控件：把一块区域切成「主区域 + 右侧面板 + 底部工具栏 + 页脚」四块，
/// 由模板部件（PART_*）承载，外部经 MainContent / RightContent / ToolBoxContent 注入内容；
/// 未注入 RightContent 时在右侧生成默认的 Enter（保存统一入口）/ Exit 按钮组。
/// </summary>
[TemplatePart(Name = "PART_MainArea", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_RightArea", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ToolBoxArea", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_FooterArea", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_RightBorder", Type = typeof(Border))]
[TemplatePart(Name = "PART_RightColumn", Type = typeof(ColumnDefinition))]
public class LayoutPageControl : Control
{
    // 模板部件引用
    private Border? _rightBorder;
    private ColumnDefinition? _rightColumn;
    private ContentPresenter? _rightArea;

    // ============ 主区域内容 ============
    public static readonly DependencyProperty MainContentProperty =
        DependencyProperty.Register("MainContent", typeof(object), typeof(LayoutPageControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public object MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    // ============ 右侧区域内容 ============
    public static readonly DependencyProperty RightContentProperty =
        DependencyProperty.Register("RightContent", typeof(object), typeof(LayoutPageControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnRightContentChanged));

    public object RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    // ============ 工具栏内容（底部左侧） ============
    public static readonly DependencyProperty ToolBoxContentProperty =
        DependencyProperty.Register("ToolBoxContent", typeof(object), typeof(LayoutPageControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public object ToolBoxContent
    {
        get => GetValue(ToolBoxContentProperty);
        set => SetValue(ToolBoxContentProperty, value);
    }

    // ============ 右侧面板可见性 ============
    // true  → 显示右侧面板，主区域占 1 列
    // false → 隐藏右侧面板，主区域跨 2 列
    public static readonly DependencyProperty IsRightPanelVisibleProperty =
        DependencyProperty.Register("IsRightPanelVisible", typeof(bool), typeof(LayoutPageControl),
            new FrameworkPropertyMetadata(true, IsRightPanelVisibleChanged));

    public bool IsRightPanelVisible
    {
        get => (bool)GetValue(IsRightPanelVisibleProperty);
        set => SetValue(IsRightPanelVisibleProperty, value);
    }

    // 兼容旧代码的 MainColumnSpan 属性（自动转换）
    public int MainColumnSpan
    {
        get => IsRightPanelVisible ? 1 : 2;
        set => IsRightPanelVisible = (value == 1);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 获取模板中的命名部件
        _rightBorder = GetTemplateChild("PART_RightBorder") as Border;
        _rightColumn = GetTemplateChild("PART_RightColumn") as ColumnDefinition;
        _rightArea = GetTemplateChild("PART_RightArea") as ContentPresenter;

        // 没有外部赋值 RightContent → 使用默认控件
        if (RightContent == null && _rightArea != null)
        {
            _rightArea.Content = CreateDefaultRightContent();
        }

        // 模板加载后立即应用当前状态
        ApplyRightPanelVisibility();
    }

    private static void IsRightPanelVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutPageControl control)
        {
            control.ApplyRightPanelVisibility();
        }
    }

    private void ApplyRightPanelVisibility()
    {
        bool visible = IsRightPanelVisible;

        if (_rightBorder != null)
        {
            _rightBorder.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_rightColumn != null)
        {
            _rightColumn.Width = visible ? new GridLength(165) : new GridLength(0);
        }

        // 主区域跨列：找 PART_MainBorder 设置 ColumnSpan
        if (GetTemplateChild("PART_MainBorder") is Border mainBorder)
        {
            Grid.SetColumnSpan(mainBorder, visible ? 1 : 2);
        }
    }

    private static void OnRightContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LayoutPageControl)d;
        if (control._rightArea == null) return;

        if (e.NewValue != null)
        {
            control._rightArea.Content = e.NewValue;
        }
        else
        {
            control._rightArea.Content = control.CreateDefaultRightContent();
        }
    }

    private FrameworkElement CreateDefaultRightContent()
    {
        var grid = new Grid();

        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });

        // ── Enter 按钮（保存的统一入口，命令走 DataContext.EnterCommand） ──
        var enterBtn = CreateRightButton(
            "EnterCommand",
            new SolidColorBrush(Color.FromRgb(0x67, 0x34, 0xFF)),
            Geometry.Parse("M232.155429 697.124571c8.704 15.579429 247.954286 287.08571401 263.16799999 313.856a24.722286 24.722286 0 0 0 43.52000001 0c11.117714-19.273143 249.051429-291.913143 261.449142-314.733714 9.033143-16.566857-0.950857-38.4-21.540571-38.40000001L661.21142901 657.846857 661.211429 364.251429 375.03999999 364.251429l1e-8 293.595428L253.80571401 657.846857c-18.65142899 0-32.658286 19.675429-21.68685701 39.241143z m429.05600001-363.52L374.601143 333.604571 374.601143 242.651429l286.610286 0 0 91.026285z m-1e-8-151.332571L374.601143 182.272 374.601143 121.6l286.610286 0 0 60.672z m1e-8-151.625143L376.429714 30.646857l0-30.354286 284.78171501 0 0 30.354286z"),
            pathWidth: 28, text: "ENTER", isBold: false, iconColumnWidth: 35);
        Grid.SetRow(enterBtn, 1);
        grid.Children.Add(enterBtn);

        // ── Exit 按钮 ──
        var exitBtn = CreateRightButton(
            "ExitCommand",
            new SolidColorBrush(Color.FromRgb(0xFF, 0xAD, 0x01)),
            Geometry.Parse("M32.704 494.464l0-423.808 205.504 0L238.208 494.464c0 172.352 94.528 317.76 205.632 317.76 113.28 0 205.568-111.36 205.568-248.32l-1e-8-69.44-136.95999999 0L632.32 241.216 752 0l101.88800001 209.92 0.96 0 0 2.112L991.99999999 494.464l-137.08799998 0L854.912 563.84C854.912 817.536 670.4 1024 443.52 1024 216.512 1024 32 786.368 32.00000001 494.464l0.70399999 0z"),
            pathWidth: 30, text: "EXIT", isBold: true, iconColumnWidth: 38, textMargin: new Thickness(5, 0, 0, 0));
        Grid.SetRow(exitBtn, 3);
        grid.Children.Add(exitBtn);

        return grid;
    }

    /// <summary>
    /// 构造右侧面板的默认按钮：IconButton 上「图标 + 文字」竖排布局，
    /// 命令按属性路径绑定到 DataContext（Enter/Exit 逻辑留在页面侧），样式取 RightButtonStyle。
    /// </summary>
    /// <param name="commandPath">DataContext 上的命令属性路径（如 "EnterCommand"）。</param>
    /// <param name="background">按钮背景色。</param>
    /// <param name="pathData">图标几何图形。</param>
    /// <param name="pathWidth">图标宽度（高度固定 30）。</param>
    /// <param name="text">按钮文字。</param>
    /// <param name="isBold">文字是否加粗。</param>
    /// <param name="iconColumnWidth">图标所在列宽。</param>
    /// <param name="textMargin">文字外边距（默认无边距）。</param>
    private IconButton CreateRightButton(
        string commandPath,
        Brush background,
        Geometry pathData,
        double pathWidth,
        string text,
        bool isBold,
        double iconColumnWidth,
        Thickness textMargin = default)
    {
        var btn = new IconButton
        {
            Background = background,
            Foreground = Brushes.White
        };
        btn.SetBinding(ButtonBase.CommandProperty, new Binding(commandPath));
        if (TryFindResource("RightButtonStyle") is Style style)
        {
            btn.Style = style;
        }

        var content = new Grid { Width = 140 };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(iconColumnWidth) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });

        var path = new Path
        {
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            Stretch = Stretch.Fill,
            Width = pathWidth,
            Height = 30,
            Data = pathData
        };
        Grid.SetColumn(path, 1);

        var label = new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = textMargin
        };
        if (isBold)
        {
            label.FontWeight = FontWeights.Bold;
        }
        Grid.SetColumn(label, 3);

        content.Children.Add(path);
        content.Children.Add(label);
        btn.Content = content;
        return btn;
    }
}
}
