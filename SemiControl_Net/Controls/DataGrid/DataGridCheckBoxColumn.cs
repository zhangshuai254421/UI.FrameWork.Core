using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SemiControl.Controls.DataGrid;

/// <summary>
/// 复选框列 — 用于显示/编辑布尔值
/// </summary>
public class GridCheckBoxColumn : GridColumn
{
    public GridCheckBoxColumn() { }
    public GridCheckBoxColumn(string header, string bindingPath, double width = 60)
    {
        Header = header;
        BindingPath = bindingPath;
        Width = width;
    }

    public override object? GetCellValue(object rowItem)
    {
        if (BindingPath == null) return null;
        var prop = rowItem.GetType().GetProperty(BindingPath);
        return prop?.GetValue(rowItem);
    }

    public override void SetCellValue(object rowItem, object? value)
    {
        if (BindingPath == null || IsReadOnly) return;
        var prop = rowItem.GetType().GetProperty(BindingPath);
        if (prop?.CanWrite == true && value is bool b)
            prop.SetValue(rowItem, b);
    }

    public override FrameworkElement GenerateDisplayElement(object rowItem)
    {
        var cb = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsEnabled = !IsReadOnly
        };
        cb.SetBinding(CheckBox.IsCheckedProperty, new Binding(BindingPath)
        {
            Mode = IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return cb;
    }

    public override FrameworkElement? GenerateEditElement(object rowItem) =>
        GenerateDisplayElement(rowItem); // 复选框直接是编辑态

    public override FrameworkElement GenerateFilterElement()
    {
        var tb = new TextBox { Height = 22, Margin = new Thickness(2), FontSize = 12 };
        tb.SetBinding(TextBox.TextProperty, new Binding(nameof(FilterText))
        {
            Source = this, Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return tb;
    }
}
