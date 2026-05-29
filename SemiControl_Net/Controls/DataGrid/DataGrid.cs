using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SemiControl.Controls.DataGrid;

/// <summary>
/// 选择模式
/// </summary>
public enum SelectionMode { Single, Multiple }

/// <summary>
/// WPF DataGrid 控件 — 参考 DevExpress GridControl 设计
/// 支持：数据绑定、自动列生成、排序、筛选行、行选择、行内编辑、列宽拖拽
/// </summary>
[TemplatePart(Name = "PART_HeaderGrid", Type = typeof(Grid))]
[TemplatePart(Name = "PART_DataRows", Type = typeof(ListBox))]
public class DataGrid : Control
{
    #region 静态构造

    static DataGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DataGrid),
            new FrameworkPropertyMetadata(typeof(DataGrid)));
    }

    public DataGrid()
    {
        Columns = new ObservableCollection<GridColumn>();
    }

    #endregion

    #region 依赖属性

    /// <summary>数据源</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable),
            typeof(DataGrid), new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>列定义（可选，不设则自动生成）</summary>
    public ObservableCollection<GridColumn> Columns
    {
        get => (ObservableCollection<GridColumn>)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(ObservableCollection<GridColumn>),
            typeof(DataGrid), new PropertyMetadata(null));

    /// <summary>当前选中项</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object),
            typeof(DataGrid), new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    /// <summary>多选时的选中项集合</summary>
    public IList? SelectedItems
    {
        get => (IList?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register(nameof(SelectedItems), typeof(IList),
            typeof(DataGrid), new PropertyMetadata(null));

    /// <summary>选择模式</summary>
    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }
    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(nameof(SelectionMode), typeof(SelectionMode),
            typeof(DataGrid), new PropertyMetadata(SelectionMode.Single));

    /// <summary>是否自动生成列</summary>
    public bool AutoGenerateColumns
    {
        get => (bool)GetValue(AutoGenerateColumnsProperty);
        set => SetValue(AutoGenerateColumnsProperty, value);
    }
    public static readonly DependencyProperty AutoGenerateColumnsProperty =
        DependencyProperty.Register(nameof(AutoGenerateColumns), typeof(bool),
            typeof(DataGrid), new PropertyMetadata(true, OnAutoGenerateColumnsChanged));

    /// <summary>是否只读</summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }
    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool),
            typeof(DataGrid), new PropertyMetadata(false));

    /// <summary>是否显示筛选行</summary>
    public bool ShowFilterRow
    {
        get => (bool)GetValue(ShowFilterRowProperty);
        set => SetValue(ShowFilterRowProperty, value);
    }
    public static readonly DependencyProperty ShowFilterRowProperty =
        DependencyProperty.Register(nameof(ShowFilterRow), typeof(bool),
            typeof(DataGrid), new PropertyMetadata(false));

    /// <summary>搜索文本</summary>
    public string? SearchText
    {
        get => (string?)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }
    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string),
            typeof(DataGrid), new PropertyMetadata(null, OnSearchTextChanged));

    #endregion

    #region 路由事件

    /// <summary>选中项变更事件</summary>
    public static readonly RoutedEvent SelectedItemChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectedItemChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<object?>), typeof(DataGrid));

    public event RoutedPropertyChangedEventHandler<object?> SelectedItemChanged
    {
        add => AddHandler(SelectedItemChangedEvent, value);
        remove => RemoveHandler(SelectedItemChangedEvent, value);
    }

    /// <summary>排序变更事件</summary>
    public static readonly RoutedEvent SortChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SortChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(DataGrid));

    public event RoutedEventHandler SortChanged
    {
        add => AddHandler(SortChangedEvent, value);
        remove => RemoveHandler(SortChangedEvent, value);
    }

    #endregion

    #region 内部状态

    /// <summary>内部视图（经排序/筛选/分组后的数据）</summary>
    private readonly ObservableCollection<object> _viewItems = new();

    /// <summary>原始数据列表（支持 INotifyCollectionChanged）</summary>
    private IEnumerable? _rawSource;

    /// <summary>当前排序列</summary>
    private GridColumn? _sortColumn;

    /// <summary>当前编辑的单元格所在行</summary>
    private object? _editingItem;

    /// <summary>模板部件</summary>
    private Grid? _headerGrid;
    private Grid? _filterGrid;
    private ListBox? _dataRows;

    #endregion

    #region RowData 附加属性（用于动态构建行单元格）

    /// <summary>附加属性：绑定的行数据，触发动态构建行内单元格</summary>
    public static readonly DependencyProperty RowDataProperty =
        DependencyProperty.RegisterAttached("RowData", typeof(object), typeof(DataGrid),
            new PropertyMetadata(null, OnRowDataChanged));

    public static object? GetRowData(DependencyObject obj) => obj.GetValue(RowDataProperty);
    public static void SetRowData(DependencyObject obj, object? value) => obj.SetValue(RowDataProperty, value);

    private static void OnRowDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Grid rowGrid && e.NewValue != null)
        {
            // 通过 VisualTreeHelper 向上查找所属 DataGrid
            var grid = FindParentDataGrid(rowGrid);
            grid?.BuildRowCells(rowGrid, e.NewValue);
        }
    }

    private static DataGrid? FindParentDataGrid(DependencyObject child)
    {
        var current = VisualTreeHelper.GetParent(child);
        while (current != null)
        {
            if (current is DataGrid dg) return dg;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    #endregion

    #region 重写 OnApplyTemplate

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _headerGrid = GetTemplateChild("PART_HeaderGrid") as Grid;
        _filterGrid = GetTemplateChild("PART_FilterGrid") as Grid;
        _dataRows = GetTemplateChild("PART_DataRows") as ListBox;

        if (_dataRows != null)
        {
            _dataRows.ItemsSource = _viewItems;
            _dataRows.SelectionChanged += OnDataRowsSelectionChanged;
            _dataRows.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnRowButtonClick));
        }

        // 构建表头与筛选行
        BuildHeaders();
        BuildFilterRow();

        // 如果数据源在模板加载前已设置，现在加载
        if (_rawSource != null && _viewItems.Count == 0)
            LoadData();

        // 注册列集合变更
        Columns.CollectionChanged += (s, e) =>
        {
            BuildHeaders();
            BuildFilterRow();
        };
    }

    #endregion

    #region 动态构建表头

    /// <summary>动态生成列头：每列 = 标题按钮 + 分隔线</summary>
    private void BuildHeaders()
    {
        if (_headerGrid == null) return;

        _headerGrid.Children.Clear();
        _headerGrid.ColumnDefinitions.Clear();

        for (int i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            if (!col.IsVisible) continue;

            // 列定义
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(col.ActualWidth),
                MinWidth = 40
            });

            // 标题按钮（点击排序）
            var headerBtn = new Button
            {
                Content = CreateHeaderContent(col),
                Height = 34,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = col
            };
            headerBtn.Click += (s, e) =>
            {
                if (s is Button btn && btn.Tag is GridColumn column)
                    OnColumnHeaderClick(column);
            };

            Grid.SetColumn(headerBtn, i);
            _headerGrid.Children.Add(headerBtn);

            // 列宽拖拽分隔线
            if (i < Columns.Count - 1)
            {
                var splitter = new Thumb
                {
                    Width = 4,
                    Cursor = Cursors.SizeWE,
                    Tag = col,
                    Background = Brushes.Transparent
                };
                splitter.DragDelta += OnColumnSplitterDrag;
                Grid.SetColumn(splitter, i);
                splitter.HorizontalAlignment = HorizontalAlignment.Right;
                _headerGrid.Children.Add(splitter);
            }
        }
    }

    /// <summary>创建列头内容（标题 + 排序箭头）</summary>
    private static StackPanel CreateHeaderContent(GridColumn col)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 8, 0) };
        sp.Children.Add(new TextBlock { Text = col.Header, VerticalAlignment = VerticalAlignment.Center });

        var arrow = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4))
        };
        arrow.SetBinding(TextBlock.TextProperty, new Binding(nameof(GridColumn.SortIcon)) { Source = col });
        sp.Children.Add(arrow);
        return sp;
    }

    /// <summary>列宽拖拽处理</summary>
    private void OnColumnSplitterDrag(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not GridColumn col) return;

        double newWidth = Math.Max(40, col.ActualWidth + e.HorizontalChange);
        col.ActualWidth = newWidth;

        // 更新表头列宽
        if (_headerGrid != null)
        {
            int idx = _headerGrid.Children.IndexOf(thumb);
            if (idx >= 0 && idx < _headerGrid.ColumnDefinitions.Count)
                _headerGrid.ColumnDefinitions[idx].Width = new GridLength(newWidth);
        }
    }

    #endregion

    #region 动态构建筛选行

    /// <summary>动态生成筛选行</summary>
    private void BuildFilterRow()
    {
        if (_filterGrid == null) return;

        _filterGrid.Children.Clear();
        _filterGrid.ColumnDefinitions.Clear();

        if (!ShowFilterRow)
        {
            _filterGrid.Visibility = Visibility.Collapsed;
            return;
        }
        _filterGrid.Visibility = Visibility.Visible;

        for (int i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            if (!col.IsVisible || col.IsReadOnly && col is not GridTextColumn) continue;

            _filterGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(col.ActualWidth),
                MinWidth = 40
            });

            var filterElement = col.GenerateFilterElement();
            if (filterElement is TextBox tb)
            {
                tb.TextChanged += (s, e) => OnFilterTextChanged();
            }
            Grid.SetColumn(filterElement, i);
            _filterGrid.Children.Add(filterElement);
        }
    }

    #endregion

    #region 动态构建数据行单元格

    /// <summary>为指定行 Grid 构建单元格</summary>
    internal void BuildRowCells(Grid rowGrid, object rowData)
    {
        rowGrid.Children.Clear();
        rowGrid.ColumnDefinitions.Clear();

        for (int i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            if (!col.IsVisible) continue;

            rowGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(col.ActualWidth),
                MinWidth = 40
            });

            var cellBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE4, 0xE8)),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(0),
                Background = Brushes.Transparent
            };

            // 根据是否编辑态选择显示或编辑元素
            if (IsEditing(rowData) && !col.IsReadOnly)
            {
                var editEl = col.GenerateEditElement(rowData);
                if (editEl != null)
                    cellBorder.Child = editEl;
                else
                    cellBorder.Child = col.GenerateDisplayElement(rowData);
            }
            else
            {
                cellBorder.Child = col.GenerateDisplayElement(rowData);
            }

            Grid.SetColumn(cellBorder, i);
            rowGrid.Children.Add(cellBorder);
        }
    }

    #endregion

    #region 列构建

    /// <summary>从数据源自动生成列</summary>
    private void AutoGenerateColumnsFromSource()
    {
        if (!AutoGenerateColumns || _rawSource == null) return;

        Columns.Clear();

        if (_rawSource is IEnumerable en)
        {
            var enumerator = en.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;
                if (first != null)
                {
                    foreach (var prop in first.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (!prop.CanRead) continue;
                        if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                            Columns.Add(new GridCheckBoxColumn(prop.Name, prop.Name, 70));
                        else
                            Columns.Add(new GridTextColumn(prop.Name, prop.Name, 130));
                    }
                }
            }
        }
    }

    #endregion

    #region 数据加载与刷新

    private void LoadData()
    {
        _viewItems.Clear();
        if (_rawSource == null) return;

        foreach (var item in _rawSource)
        {
            if (PassesAllFilters(item))
                _viewItems.Add(item);
        }

        ApplyCurrentSort();
    }

    /// <summary>判断行是否通过所有列筛选和全局搜索</summary>
    private bool PassesAllFilters(object item)
    {
        // 全局搜索
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            bool found = false;
            foreach (var col in Columns)
            {
                if (col.PassesSearch(item, SearchText!))
                { found = true; break; }
            }
            if (!found) return false;
        }

        // 列筛选
        foreach (var col in Columns)
        {
            if (!col.PassesFilter(item))
                return false;
        }
        return true;
    }

    /// <summary>应用当前排序</summary>
    private void ApplyCurrentSort()
    {
        if (_sortColumn == null || _sortColumn.SortState == SortState.None) return;

        var sorted = _sortColumn.SortState == SortState.Ascending
            ? _viewItems.OrderBy(item => _sortColumn.GetCellValue(item)?.ToString())
            : _viewItems.OrderByDescending(item => _sortColumn.GetCellValue(item)?.ToString());

        var sortedList = sorted.ToList();
        _viewItems.Clear();
        foreach (var item in sortedList)
            _viewItems.Add(item);
    }

    /// <summary>刷新视图（筛选/搜索文本变化时）</summary>
    private void RefreshView()
    {
        // 保存选中项
        var selected = SelectedItem;
        LoadData();
        if (selected != null && _viewItems.Contains(selected))
            SelectedItem = selected;
    }

    #endregion

    #region 排序逻辑

    /// <summary>点击列头触发排序</summary>
    internal void OnColumnHeaderClick(GridColumn column)
    {
        if (_sortColumn != null && _sortColumn != column)
            _sortColumn.SortState = SortState.None;

        _sortColumn = column;
        column.SortState = column.SortState switch
        {
            SortState.None => SortState.Ascending,
            SortState.Ascending => SortState.Descending,
            SortState.Descending => SortState.None,
            _ => SortState.Ascending
        };

        LoadData();
        RaiseEvent(new RoutedEventArgs(SortChangedEvent));
    }

    #endregion

    #region 筛选行变更

    internal void OnFilterTextChanged()
    {
        RefreshView();
    }

    #endregion

    #region 选中逻辑

    private void OnDataRowsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_dataRows == null) return;

        if (SelectionMode == SelectionMode.Single)
        {
            SelectedItem = _dataRows.SelectedItem;
        }
        else
        {
            SelectedItems = _dataRows.SelectedItems;
        }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (DataGrid)d;
        if (grid._dataRows != null && grid._dataRows.SelectedItem != e.NewValue)
            grid._dataRows.SelectedItem = e.NewValue;

        grid.RaiseEvent(new RoutedPropertyChangedEventArgs<object?>(
            e.OldValue, e.NewValue, SelectedItemChangedEvent));
    }

    #endregion

    #region 行内编辑

    /// <summary>捕获行内按钮点击，触发编辑等操作</summary>
    private void OnRowButtonClick(object sender, RoutedEventArgs e)
    {
        // 预留：可用于处理行内自定义按钮
    }

    /// <summary>开始编辑某行</summary>
    public void BeginEdit(object rowItem)
    {
        _editingItem = rowItem;
        // 刷新视图以切换显示/编辑模板
        if (_dataRows != null)
            _dataRows.Items.Refresh();
    }

    /// <summary>提交编辑</summary>
    public void CommitEdit()
    {
        _editingItem = null;
        _dataRows?.Items.Refresh();
    }

    /// <summary>判断某行是否处于编辑态</summary>
    internal bool IsEditing(object rowItem) => _editingItem == rowItem;

    #endregion

    #region 属性变更回调

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (DataGrid)d;
        grid._rawSource = e.NewValue as IEnumerable;

        // 自动生成列
        if (grid.AutoGenerateColumns && grid.Columns.Count == 0)
            grid.AutoGenerateColumnsFromSource();

        grid.LoadData();

        // 监听集合变更
        if (e.OldValue is INotifyCollectionChanged oldNcc)
            oldNcc.CollectionChanged -= grid.OnSourceCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newNcc)
            newNcc.CollectionChanged += grid.OnSourceCollectionChanged;
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshView();
    }

    private static void OnAutoGenerateColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (DataGrid)d;
        if ((bool)e.NewValue && grid._rawSource != null)
        {
            grid.AutoGenerateColumnsFromSource();
            grid.LoadData();
        }
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((DataGrid)d).RefreshView();
    }

    #endregion
}
