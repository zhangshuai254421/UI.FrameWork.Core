using Framework.Core.Common;
using Prism.Events;
using UI.FrameWork.Core.Common;
using UI.FrameWork.Core.Events;
using UI.FrameWork.Core.Main.View;

namespace UI.FrameWork.Core.Main
{
    public class DirectViewModel : BindableBase, IJournalAware
    {
        private readonly DelegateCommand _btnShowLogCommand = null!;
        public DelegateCommand BtnShowLogCommand => _btnShowLogCommand ?? new DelegateCommand(ShowLog);

        private readonly PrismRegionNavigator _regionNavigator;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;

        public DirectViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _regionNavigator = regionNavigator;
            _eventAggregator = eventAggregator;
        }

        void ShowLog()
        {
            //_regionManager.RequestNavigate(RegionNames.BaseViewMainRegion, nameof(NumberKeyPadView));
            //Task.Delay(1000);
            IoC.Get<INavigationService>().NavigateToAsync(nameof(LogViewerView));
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
