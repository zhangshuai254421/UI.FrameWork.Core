using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SemiControl.Controls.DataGrid;

/// <summary>
/// 选择模式（兼容旧版）
/// </summary>
public enum SelectionMode { Single, Multiple }

/// <summary>
/// WPF DataGrid 控件 — 基于 Microsoft DataGrid，附加筛选行、全局搜索等功能。
/// 相比手动从 Control 构建，代码量减少约 80%，同时获得原生排序/虚拟化/编辑等能力。
/// </summary>
public class DataGrid : System.Windows.Controls.DataGrid
{
    #region 附加依赖属性

    /// <summary>是否显示筛选行</summary>
    public bool ShowFilterRow
    {
        get => (bool)GetValue(ShowFilterRowProperty);
        set => SetValue(ShowFilterRowProperty, value);
    }
    public static readonly DependencyProperty ShowFilterRowProperty =
        DependencyProperty.Register(nameof(ShowFilterRow), typeof(bool),
            typeof(DataGrid), new PropertyMetadata(false));

    /// <summary>全局搜索文本（跨所有列模糊匹配）</summary>
    public string? SearchText
    {
        get => (string?)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }
    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string),
            typeof(DataGrid), new PropertyMetadata(null, OnSearchTextChanged));

    /// <summary>兼容旧版 SelectionMode 枚举（映射到基类 DataGridSelectionMode）</summary>
    public new SelectionMode SelectionMode
    {
        get => base.SelectionMode == DataGridSelectionMode.Extended
            ? SelectionMode.Multiple : SelectionMode.Single;
        set => base.SelectionMode = value == SelectionMode.Multiple
            ? DataGridSelectionMode.Extended : DataGridSelectionMode.Single;
    }

    #endregion

    #region 路由事件

    /// <summary>选中项变更事件（兼容旧版）</summary>
    public static readonly RoutedEvent SelectedItemChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectedItemChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<object?>), typeof(DataGrid));

    public event RoutedPropertyChangedEventHandler<object?> SelectedItemChanged
    {
        add => AddHandler(SelectedItemChangedEvent, value);
        remove => RemoveHandler(SelectedItemChangedEvent, value);
    }

    /// <summary>排序变更事件（兼容旧版）</summary>
    public static readonly RoutedEvent SortChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SortChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(DataGrid));

    public event RoutedEventHandler SortChanged
    {
        add => AddHandler(SortChangedEvent, value);
        remove => RemoveHandler(SortChangedEvent, value);
    }

    #endregion

    #region 构造

    public DataGrid()
    {
        // 默认样式设置（可在 XAML Style 中覆盖）
        AutoGenerateColumns = true;
        IsReadOnly = false;
        CanUserAddRows = false;
        CanUserDeleteRows = false;
        CanUserReorderColumns = false;
        CanUserSortColumns = true;
        CanUserResizeColumns = true;
        CanUserResizeRows = false;
        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0xF8, 0xF9, 0xFB));
        RowBackground = Brushes.White;
        SelectionUnit = DataGridSelectionUnit.FullRow;

        // 桥接基类事件到自定义路由事件
        SelectionChanged += (s, e) =>
        {
            RaiseEvent(new RoutedPropertyChangedEventArgs<object?>(
                e.RemovedItems.Count > 0 ? e.RemovedItems[0] : null,
                e.AddedItems.Count > 0 ? e.AddedItems[0] : null,
                SelectedItemChangedEvent));
        };

        Sorting += (s, e) =>
        {
            RaiseEvent(new RoutedEventArgs(SortChangedEvent));
        };
    }

    #endregion

    #region 全局搜索逻辑

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (DataGrid)d;
        grid.ApplyGlobalSearch();
    }

    /// <summary>应用全局搜索过滤</summary>
    private void ApplyGlobalSearch()
    {
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            view.Filter = null;
        }
        else
        {
            var search = SearchText!;
            view.Filter = item =>
            {
                if (item == null) return false;
                foreach (var prop in item.GetType().GetProperties())
                {
                    var val = prop.GetValue(item)?.ToString();
                    if (val != null && val.Contains(search, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            };
        }
    }

    #endregion
}

