// Controls/LayoutPageControl.cs
using System.Windows;
using System.Windows.Controls;

namespace UI.FrameWork.Core.Main.View
{ 
[TemplatePart(Name = "PART_MainArea", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_RightArea", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ToolBoxArea", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_FooterArea", Type = typeof(ContentPresenter))]
public class LayoutPageControl : Control
{
    static LayoutPageControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(LayoutPageControl),
            new FrameworkPropertyMetadata(typeof(LayoutPageControl)));
    }

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
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

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
        // 预留：可在此获取 PART_ 命名元素做进一步操作
    }
}

}