using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.FrameWork.Core.Common;

namespace UI.FrameWork.Core.Main
{
    public class DirectViewModel :BindableBase
    {
        private readonly DelegateCommand _btnShowLogCommand = null!;
        public DelegateCommand BtnShowLogCommand => _btnShowLogCommand ?? new DelegateCommand(ShowLog);

        private readonly PrismRegionNavigator _regionNavigator;
        private readonly IRegionManager _regionManager;
        public DirectViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator)
        {
            this._regionManager = regionManager;
            _regionNavigator = regionNavigator;
        }
        void ShowLog()
        {
            // 获取目标区域
            IRegion targetRegion = _regionManager.Regions["ToolBoxRegion"];


            _regionManager.RequestNavigate("MainRegion", nameof(LogViewerView));

        }
    }
}
