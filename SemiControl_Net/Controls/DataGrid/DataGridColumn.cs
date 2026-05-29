using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SemiControl.Controls.DataGrid;

/// <summary>
/// 列排序状态
/// </summary>
public enum SortState { None, Ascending, Descending }

/// <summary>
/// DataGrid 列基类 — 定义列的公共属性与行为
/// </summary>
public abstract class GridColumn : DependencyObject, INotifyPropertyChanged
{
    private SortState _sortState;
    private string? _filterText;
    private double _actualWidth = 120;
    private bool _isVisible = true;

    /// <summary>列标题</summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>绑定的属性路径</summary>
    public string? BindingPath { get; set; }

    /// <summary>列宽</summary>
    public double Width { get; set; } = 120;

    /// <summary>实际列宽（含拖拽调整）</summary>
    public double ActualWidth
    {
        get => _actualWidth;
        set { _actualWidth = value; OnPropertyChanged(nameof(ActualWidth)); }
    }

    /// <summary>是否可见</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(nameof(IsVisible)); }
    }

    /// <summary>是否只读</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>当前排序状态</summary>
    public SortState SortState
    {
        get => _sortState;
        set
        {
            _sortState = value;
            OnPropertyChanged(nameof(SortState));
            OnPropertyChanged(nameof(SortIcon));
        }
    }

    /// <summary>排序图标（用于绑定）</summary>
    public string SortIcon => SortState switch
    {
        SortState.Ascending => "▲",
        SortState.Descending => "▼",
        _ => ""
    };

    /// <summary>筛选文本</summary>
    public string? FilterText
    {
        get => _filterText;
        set { _filterText = value; OnPropertyChanged(nameof(FilterText)); }
    }

    /// <summary>获取指定行的单元格值</summary>
    public abstract object? GetCellValue(object rowItem);

    /// <summary>设置指定行的单元格值</summary>
    public abstract void SetCellValue(object rowItem, object? value);

    /// <summary>创建用于显示的 FrameworkElement</summary>
    public abstract FrameworkElement GenerateDisplayElement(object rowItem);

    /// <summary>创建用于编辑的 FrameworkElement</summary>
    public abstract FrameworkElement? GenerateEditElement(object rowItem);

    /// <summary>创建筛选输入控件</summary>
    public abstract FrameworkElement GenerateFilterElement();

    /// <summary>判断当前行是否通过筛选</summary>
    public bool PassesFilter(object rowItem)
    {
        if (string.IsNullOrWhiteSpace(FilterText)) return true;
        var val = GetCellValue(rowItem);
        return val?.ToString()?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>判断行数据是否包含全局搜索文本</summary>
    public bool PassesSearch(object rowItem, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return true;
        var val = GetCellValue(rowItem);
        return val?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
