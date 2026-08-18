using Framework.Core.Common;
using Prism.Events;
using PrismUI.Core;
using UI.FrameWork.Core.Common;
namespace UI.FrameWork.Core.Main
{
    public class DirectViewModel : BindableBase, IJournalAware
    {
        private readonly DelegateCommand _btnShowLogCommand = null!;
        public DelegateCommand BtnShowLogCommand => _btnShowLogCommand ?? new DelegateCommand(ShowLog);

        private readonly PrismRegionNavigator _regionNavigator;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly INavigationService _navigationService;

        public DirectViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator, IEventAggregator eventAggregator, INavigationService navigationService)
        {
            _regionManager = regionManager;
            _regionNavigator = regionNavigator;
            _eventAggregator = eventAggregator;
            _navigationService = navigationService;

        }

        void ShowLog()
        {
            //_regionManager.RequestNavigate(RegionNames.BaseViewMainRegion, nameof(NumberKeyPadView));
            //Task.Delay(1000);
            _navigationService.NavigateToAsync(ViewNames.LogViewerView);
            //_regionManager.RequestNavigate(RegionNames.BaseViewMainRegion, nameof(LogViewerView));
           
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
