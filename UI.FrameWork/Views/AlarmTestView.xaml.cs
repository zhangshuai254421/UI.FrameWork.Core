using System.Windows.Controls;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：报警测试页（#15）——真引擎演示装置喂报警列表（主列表/搁置清单/详情档案），
    //       验证 Framework.Alarm 全链路。交互全部走 AlarmTestViewModel 绑定，代码后置只做初始化。
    public partial class AlarmTestView : UserControl
    {
        /// <summary>构造：初始化界面元素（Prism 按注册装配 AlarmTestViewModel 作数据上下文）。</summary>
        public AlarmTestView()
        {
            InitializeComponent();
        }
    }
}
