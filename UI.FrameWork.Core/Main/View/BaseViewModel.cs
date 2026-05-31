using UI.FrameWork.Core.Events;

namespace UI.FrameWork.Core.Main.View
{
    /// <summary>
    /// BaseView 的 ViewModel — 控制主区域列跨和工具栏区域可见性。
    /// 通过 EventAggregator 订阅 LayoutModeChangedEvent 来切换布局，与其他 ViewModel 解耦。
    /// </summary>
    public class BaseViewModel : BindableBase
    {
        private int _mainColumnSpan = 1;
        /// <summary>主区域列跨（宽度）：1=占第一列，2=占满两列</summary>
        public int MainColumnSpan
        {
            get => _mainColumnSpan;
            set => SetProperty(ref _mainColumnSpan, value);
        }

        public BaseViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<LayoutModeChangedEvent>().Subscribe(isFullWidth =>
            {
                MainColumnSpan = isFullWidth ? 2 : 1;
            });
        }
    }
}
