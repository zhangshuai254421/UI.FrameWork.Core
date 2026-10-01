using System.Collections.Generic;
using System.Windows.Controls;

namespace SemiAppliaction.Views
{
    /// <summary>检测测试页：选图后运行 YOLO 各类推理，并把结果叠加到图像查看控件。</summary>
    public partial class DetectionTestView : UserControl
    {
        /// <summary>空构造：初始化 XAML 组件。</summary>
        public DetectionTestView()
        {
            InitializeComponent();
        }
    }
}
