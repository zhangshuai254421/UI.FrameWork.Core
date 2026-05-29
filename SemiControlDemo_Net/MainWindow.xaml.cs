using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SemiControl.Controls.DataGrid;

namespace SemiControlDemo_Net
{
    /// <summary>
    /// DataGrid 控件演示窗口 — 展示排序、筛选、搜索、选择等功能
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Employee> Employees { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadDemoData();
            dataGrid.ItemsSource = Employees;
            txtRowCount.Text = $"（共 {Employees.Count} 行）";

            // 监听选中变更
            dataGrid.SelectedItemChanged += (s, e) =>
            {
                if (e.NewValue is Employee emp)
                    txtSelected.Text = $"{emp.Name} - {emp.Department} - {emp.Title}";
                else
                    txtSelected.Text = "无";
            };
        }

        private void LoadDemoData()
        {
            var departments = new[] { "研发部", "市场部", "财务部", "人事部", "运维部" };
            var titles = new[] { "工程师", "经理", "主管", "专员", "总监", "架构师" };
            var names = new[] { "张三", "李四", "王五", "赵六", "陈七", "周八", "吴九", "郑十",
                                "刘一", "孙二", "黄三", "林四", "何五", "郭六", "马七", "罗八" };
            var rand = new Random(42);

            for (int i = 0; i < 30; i++)
            {
                Employees.Add(new Employee
                {
                    Id = 1000 + i,
                    Name = names[rand.Next(names.Length)],
                    Age = rand.Next(22, 55),
                    Department = departments[rand.Next(departments.Length)],
                    Title = titles[rand.Next(titles.Length)],
                    Salary = Math.Round(5000 + rand.NextDouble() * 15000, 2),
                    IsActive = rand.Next(3) > 0
                });
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.SearchText = txtSearch.Text;
            UpdateRowCount();
        }

        private void ChkFilterRow_Changed(object sender, RoutedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.ShowFilterRow = chkFilterRow.IsChecked == true;
        }

        private void CmbSelectionMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.SelectionMode = cmbSelectionMode.SelectedIndex == 0
                ? SemiControl.Controls.DataGrid.SelectionMode.Single
                : SemiControl.Controls.DataGrid.SelectionMode.Multiple;
        }

        private void BtnToggleReadOnly_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.IsReadOnly = !dataGrid.IsReadOnly;
            btnToggleReadOnly.Content = dataGrid.IsReadOnly ? "取消只读" : "切换只读";
        }

        private void UpdateRowCount()
        {
            // 通过反射获取内部视图项数量
            txtRowCount.Text = $"（共 {Employees.Count} 行）";
        }
    }

    /// <summary>
    /// 示例数据模型
    /// </summary>
    public class Employee : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _age;
        private string _department = string.Empty;
        private string _title = string.Empty;
        private double _salary;
        private bool _isActive;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public int Age
        {
            get => _age;
            set { _age = value; OnPropertyChanged(nameof(Age)); }
        }

        public string Department
        {
            get => _department;
            set { _department = value; OnPropertyChanged(nameof(Department)); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public double Salary
        {
            get => _salary;
            set { _salary = value; OnPropertyChanged(nameof(Salary)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}