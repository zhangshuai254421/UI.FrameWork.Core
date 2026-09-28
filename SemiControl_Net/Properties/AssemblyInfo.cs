using System.Windows.Markup;

// XML 命名空间映射：XAML 中 xmlns 前缀指向 https://github.io/semicontrol 时，
// 下列 CLR 命名空间内的类型均可直接使用（无需逐个写 clr-namespace）。
[assembly: XmlnsDefinition("https://github.io/semicontrol", "SemiControl.Controls")]
[assembly: XmlnsDefinition("https://github.io/semicontrol", "SemiControl.Controls.DataGrid")]
[assembly: XmlnsDefinition("https://github.io/semicontrol", "SemiControl.Data")]
[assembly: XmlnsDefinition("https://github.io/semicontrol", "SemiControl.Tools")]
