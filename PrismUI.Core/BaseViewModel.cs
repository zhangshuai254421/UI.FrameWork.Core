
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
        #region 字段

        protected readonly ILogger Logger;  // 非泛型 ILogger，但内部类别名是子类的
        /// <summary>退出命令的缓存实例，首次访问 ExitCommand 时创建。</summary>
        private DelegateCommand _exitCommand = null!;
        /// <summary>进入命令的缓存实例，首次访问 EnterCommand 时创建。</summary>
        public DelegateCommand _enterCommand = null!;

        #endregion

        #region 构造函数


        /// <summary>无参构造：从 IoC 容器解析日志工厂与事件聚合器，日志类别名为实际子类类型。</summary>
        public BaseViewModel()
        {
            Logger = IoC.Get< ILoggerFactory >().CreateLogger(GetType());  // ← GetType() 返回实际子类类型
            EventAggregator = IoC.Get <IEventAggregator>();
        }

        #endregion

        #region 属性

        /// <summary>事件聚合器，用于发布与订阅跨 ViewModel 的事件。</summary>
        public IEventAggregator EventAggregator { get; set; }

        #endregion

        #region 命令

        /// <summary>退出命令：执行时调用 ExitCommandExecute，默认导航返回上一页。</summary>
        public  DelegateCommand ExitCommand => _exitCommand ?? new DelegateCommand(() =>
        {
            // 这里可以放置你想要执行的逻辑

            ExitCommandExecute();
        });

        /// <summary>进入命令：执行时调用 EnterCommandExecute，基类默认无操作。</summary>
        public  DelegateCommand EnterCommand => _enterCommand ?? new DelegateCommand(() =>
        {
            EnterCommandExecute();
            // 这里可以放置你想要执行的逻辑

        });

        #endregion

        #region 方法

        /// <summary>进入命令的执行逻辑，基类默认留空，子类按需重写。</summary>
        public virtual void EnterCommandExecute()
        {
            //Logger?.LogInformation("Navigating to recipe context view.");
            // 这里可以放置你想要执行的逻辑
            //IoC.Get<INavigationService>().NavigateToAsync(ViewNames.RecipeContextView, RegionNames.MainRegion);
        }
        /// <summary>退出命令的执行逻辑，默认导航返回上一页。</summary>
        public virtual void ExitCommandExecute()
        {
            // 这里可以放置你想要执行的逻辑
            IoC.Get<INavigationService>().GoBack();
        }

        #endregion
    }
}
