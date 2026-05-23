using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UI.FrameWork.Core.CustomControls
{
    /// <summary>
    /// TimeBlock.xaml 的交互逻辑
    /// </summary>
    public partial class TimeBlock : System.Windows.Controls.UserControl
    {
        // Token: 0x04000194 RID: 404
        private readonly DispatcherTimer _dispatcherTimer;

        // Token: 0x04000195 RID: 405
        private bool _isDisposed;

        // Token: 0x04000196 RID: 406
        public static readonly DependencyProperty DisplayTimeProperty = DependencyProperty.Register("DisplayTime", typeof(DateTime), typeof(TimeBlock), new PropertyMetadata(default(DateTime)));
        public DateTime DisplayTime
        {
            get
            {
                return (DateTime)base.GetValue(TimeBlock.DisplayTimeProperty);
            }
            set
            {
                base.SetValue(TimeBlock.DisplayTimeProperty, value);
            }
        }
        // Token: 0x04000197 RID: 407
        internal TimeBlock US;

        // Token: 0x04000198 RID: 408

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            this.DisplayTime = DateTime.Now;
        }
        public TimeBlock()
        {
            InitializeComponent();
            this._dispatcherTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(200.0)
            };
            this._dispatcherTimer.Tick += this.DispatcherTimer_Tick;
            this._dispatcherTimer.Start();
        }
        public void Dispose()
        {
            bool isDisposed = this._isDisposed;
            if (!isDisposed)
            {
                this._dispatcherTimer.Stop();
                this._dispatcherTimer.Tick -= this.DispatcherTimer_Tick;
                this._isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
