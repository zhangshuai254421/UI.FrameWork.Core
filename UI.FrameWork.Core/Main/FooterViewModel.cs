using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UI.FrameWork.Core.Common;

namespace UI.FrameWork.Core.Main
{
    public class FooterViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator) : BindableBase
    {
        private readonly IRegionManager _regionManager = regionManager;
        private readonly PrismRegionNavigator _regionNavigator = regionNavigator;
        private  readonly DelegateCommand _btnNumberKeyPadCommand = null!;

        public DelegateCommand BtnNumberKeyPadCommand => _btnNumberKeyPadCommand ??new DelegateCommand(BtnNumberKeyPad);

        void BtnNumberKeyPad()
        {
            regionManager.RequestNavigate("ToolBoxRegion", nameof(NumberKeyPadView));
            //_regionNavigator.NavigateAsync("ToolBoxRegion", "HeaderView");
        }
    }
}
