using System.Windows.Controls;

namespace Framework.Alarm.UI
{
    /// <summary>
    /// AlarmListView.xaml 的交互逻辑。不设 AutoWireViewModel： DataContext 由两种方式供给——
    /// 导航进入时 Prism 按 RegisterForNavigation 配对（AlarmListView ↔ AlarmListViewModel）；
    /// 被宿主演示页嵌入时外部DataContext 注入（AlarmTestView 里 DataContext="{Binding List}"）。
    /// </summary>
    public partial class AlarmListView : UserControl
    {
        public AlarmListView()
        {
            InitializeComponent();
        }
    }
}
