using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SemiControl.Controls.DataGrid;

/// <summary>
/// 文本列 — 支持字符串/数值的编辑与显示
/// </summary>
public class GridTextColumn : GridColumn
{
    public GridTextColumn() { }
    public GridTextColumn(string header, string bindingPath, double width = 120)
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
        if (prop?.CanWrite == true)
        {
            var converted = ConvertValue(value, prop.PropertyType);
            prop.SetValue(rowItem, converted);
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        if (targetType.IsAssignableFrom(value.GetType())) return value;
        try { return Convert.ChangeType(value, targetType); }
        catch { return value; }
    }

    public override FrameworkElement GenerateDisplayElement(object rowItem)
    {
        var tb = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 6, 0)
        };
        tb.SetBinding(TextBlock.TextProperty, new Binding(BindingPath));
        return tb;
    }

    public override FrameworkElement? GenerateEditElement(object rowItem)
    {
        if (IsReadOnly) return null;
        var tb = new TextBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0),
            BorderThickness = new Thickness(0),
            Height = 22
        };
        tb.SetBinding(TextBox.TextProperty, new Binding(BindingPath)
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return tb;
    }

    public override FrameworkElement GenerateFilterElement()
    {
        var tb = new TextBox
        {
            Height = 22,
            Margin = new Thickness(2),
            FontSize = 12
        };
        tb.SetBinding(TextBox.TextProperty, new Binding(nameof(FilterText))
        {
            Source = this,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return tb;
    }
}
