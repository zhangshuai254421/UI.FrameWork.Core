using Framework.Core.Common;
using Framework.Device.Domain;
using Prism.Events;
using PrismUI.Core;
using UI.FrameWork.Core.Common;
namespace UI.FrameWork.Core.Main
{
    /// <summary>页脚直连按钮区 ViewModel：提供打开日志查看页的导航命令。</summary>
    public class DirectViewModel : BindableBase, IJournalAware
    {
        /// <summary>查看日志命令的缓存实例，保证命令对象只创建一次。</summary>
        private readonly DelegateCommand _btnShowLogCommand = null!;
        /// <summary>查看日志命令：绑定页脚按钮，点击后导航到日志查看页。</summary>
        public DelegateCommand BtnShowLogCommand => _btnShowLogCommand ?? new DelegateCommand(ShowLog);

        /// <summary>带统一错误处理的区域导航器（当前未直接使用，保留备用）。</summary>
        private readonly PrismRegionNavigator _regionNavigator;
        /// <summary>Prism 区域管理器（当前未直接使用，保留备用）。</summary>
        private readonly IRegionManager _regionManager;
        /// <summary>事件聚合器（当前未直接使用，保留备用）。</summary>
        private readonly IEventAggregator _eventAggregator;
        /// <summary>应用级导航服务，实际执行页面跳转。</summary>
        private readonly INavigationService _navigationService;

        /// <summary>注入导航所需的区域、事件与导航服务。</summary>
        public DirectViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator, IEventAggregator eventAggregator, INavigationService navigationService)
        {
            _regionManager = regionManager;
            _regionNavigator = regionNavigator;
            _eventAggregator = eventAggregator;
            _navigationService = navigationService;

        }

        /// <summary>跳转到日志查看页。</summary>
        void ShowLog()
        {
            //_regionManager.RequestNavigate(RegionNames.BaseViewMainRegion, nameof(NumberKeyPadView));
            //Task.Delay(1000);
            _navigationService.NavigateToAsync(ViewNames.LogViewerView);

        }

        /// <summary>
        /// 不作为导航历史的一部分，点击后无法通过导航历史返回到之前的页面
        /// </summary>
        /// <returns></returns>
        public bool PersistInHistory()
        {
            return false;
        }
    }
}
