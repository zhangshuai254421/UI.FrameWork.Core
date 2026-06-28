// Controls/LayoutPageControl.cs
using System.Windows;
using System.Windows.Controls;

namespace SemiControl.Controls
{
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
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

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
}
}
