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
        /// <summary>演示用员工数据集合，作为 DataGrid 的数据源。</summary>
        public ObservableCollection<Employee> Employees { get; } = new();

        /// <summary>构造函数：加载演示数据、绑定数据源并监听选中变更事件。</summary>
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

        /// <summary>用固定随机种子生成 30 条员工演示数据，保证每次运行展示一致。</summary>
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

        /// <summary>搜索框文本变化：把关键字同步给控件的 SearchText 并刷新行数显示。</summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.SearchText = txtSearch.Text;
            UpdateRowCount();
        }

        /// <summary>筛选行复选框变化：切换控件的筛选行显隐。</summary>
        private void ChkFilterRow_Changed(object sender, RoutedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.ShowFilterRow = chkFilterRow.IsChecked == true;
        }

        /// <summary>选择模式下拉框变化：在单选与多选之间切换控件的选择模式。</summary>
        private void CmbSelectionMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.SelectionMode = cmbSelectionMode.SelectedIndex == 0
                ? SemiControl.Controls.DataGrid.SelectionMode.Single
                : SemiControl.Controls.DataGrid.SelectionMode.Multiple;
        }

        /// <summary>只读切换按钮：切换控件只读状态并同步按钮文案。</summary>
        private void BtnToggleReadOnly_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid == null) return;
            dataGrid.IsReadOnly = !dataGrid.IsReadOnly;
            btnToggleReadOnly.Content = dataGrid.IsReadOnly ? "取消只读" : "切换只读";
        }

        /// <summary>ImageViewer 演示按钮：打开 ImageViewer 演示窗口并设置本窗口为属主。</summary>
        private void BtnImageViewerDemo_Click(object sender, RoutedEventArgs e)
        {
            new ImageViewerDemoWindow { Owner = this }.Show();
        }

        /// <summary>刷新行数文本显示。</summary>
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
        /// <summary>员工姓名的后备字段。</summary>
        private string _name = string.Empty;
        /// <summary>员工年龄的后备字段。</summary>
        private int _age;
        /// <summary>所属部门的后备字段。</summary>
        private string _department = string.Empty;
        /// <summary>职位的后备字段。</summary>
        private string _title = string.Empty;
        /// <summary>薪资的后备字段。</summary>
        private double _salary;
        /// <summary>在职状态的后备字段。</summary>
        private bool _isActive;

        /// <summary>员工编号。</summary>
        public int Id { get; set; }

        /// <summary>员工姓名，变更时通知界面。</summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>员工年龄，变更时通知界面。</summary>
        public int Age
        {
            get => _age;
            set { _age = value; OnPropertyChanged(nameof(Age)); }
        }

        /// <summary>所属部门，变更时通知界面。</summary>
        public string Department
        {
            get => _department;
            set { _department = value; OnPropertyChanged(nameof(Department)); }
        }

        /// <summary>职位，变更时通知界面。</summary>
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        /// <summary>薪资，变更时通知界面。</summary>
        public double Salary
        {
            get => _salary;
            set { _salary = value; OnPropertyChanged(nameof(Salary)); }
        }

        /// <summary>在职状态，变更时通知界面。</summary>
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        /// <summary>属性变更事件，供 WPF 绑定引擎监听。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>触发属性变更通知的标准辅助方法。</summary>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}