
namespace PrismUI.Core
{
    /// <summary>
    /// BaseView 的 ViewModel — 控制主区域列跨和工具栏区域可见性。
    /// 通过 EventAggregator 订阅 LayoutModeChangedEvent 来切换布局，与其他 ViewModel 解耦。
    /// </summary>
    public class BaseViewModel : BindableBase
    {
        public IEventAggregator EventAggregator { get; set; }


        public BaseViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;

        }
    }
}
