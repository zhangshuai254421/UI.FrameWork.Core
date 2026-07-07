
using Framework.Core.Common;
using Microsoft.Extensions.Logging;

namespace PrismUI.Core
{
    /// <summary>
    /// BaseView 的 ViewModel — 控制主区域列跨和工具栏区域可见性。
    /// 通过 EventAggregator 订阅 LayoutModeChangedEvent 来切换布局，与其他 ViewModel 解耦。
    /// </summary>
    public class BaseViewModel : BindableBase
    {
        protected readonly ILogger Logger;  // 非泛型 ILogger，但内部类别名是子类的
        private DelegateCommand _exitCommand = null!;
        public DelegateCommand _enterCommand = null!;

        public IEventAggregator EventAggregator { get; set; }


        public BaseViewModel(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
        {
            Logger = loggerFactory.CreateLogger(GetType());  // ← GetType() 返回实际子类类型
            EventAggregator = eventAggregator;
        }

        public BaseViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
        }

        public virtual void EnterCommandExecute()
        {
            Logger?.LogInformation("Navigating to recipe context view.");
            // 这里可以放置你想要执行的逻辑
            //IoC.Get<INavigationService>().NavigateToAsync(ViewNames.RecipeContextView, RegionNames.MainRegion);
        }
        public virtual void ExitCommandExecute()
        {
            // 这里可以放置你想要执行的逻辑
            IoC.Get<INavigationService>().GoBack();
        }

        public  DelegateCommand ExitCommand => _exitCommand ?? new DelegateCommand(() =>
        {
            // 这里可以放置你想要执行的逻辑

            ExitCommandExecute();
        });

        public  DelegateCommand EnterCommand => _enterCommand ?? new DelegateCommand(() =>
        {
            EnterCommandExecute();
            // 这里可以放置你想要执行的逻辑
           
        });
    }
}
