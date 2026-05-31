using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UI.FrameWork.Core.Common;
using UI.FrameWork.Core.Events;

namespace UI.FrameWork.Core.Main
{
    public class FooterViewModel : BindableBase
    {

        private readonly IRegionManager _regionManager;
        private readonly PrismRegionNavigator _regionNavigator;
        private readonly DelegateCommand _btnNumberKeyPadCommand = null!;
        private readonly DelegateCommand _btnDirectCommand = null!;
        private readonly DelegateCommand _btnNavigateBackCommand = null!;
        private readonly DelegateCommand _btnNavigateForwardCommand = null!;
        private readonly IRegionManager regionManager;
        private bool numberKeyPadChecked;

        public bool NumberKeyPadChecked
        {
            get => numberKeyPadChecked;
            set => SetProperty(ref numberKeyPadChecked, value);
        }

  
        private bool directChecked;

        public bool DirectChecked
        {
            get => directChecked;
            set => SetProperty(ref directChecked, value);
        }


        public FooterViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            _regionManager = regionManager;
            _regionNavigator = regionNavigator;
            eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Subscribe(isChecked =>
            {
                if (DirectChecked|| NumberKeyPadChecked) {
                    IoC.Get<INavigationService>().GoBack(RegionNames.ToolBoxRegion);
                }
                DirectChecked = isChecked ? false : true;
                NumberKeyPadChecked = isChecked ? false : true;
            });
        }

        public DelegateCommand BtnNumberKeyPadCommand => _btnNumberKeyPadCommand ??new DelegateCommand(BtnNumberKeyPad);
        public DelegateCommand BtnDirectCommand => _btnDirectCommand ?? new DelegateCommand(BtnDirect);

        public DelegateCommand BtnNavigateBackCommand => _btnNavigateBackCommand?? new DelegateCommand(() =>
        {
         
            IoC.Get<INavigationService>().GoBack();
        });

        public DelegateCommand BtnNavigateForwarddCommand => _btnNavigateForwardCommand ?? new DelegateCommand(() =>
        {

            IoC.Get<INavigationService>().GoForward();
        });

        void BtnNumberKeyPad()
        {
 
           if (!NumberKeyPadChecked)
            {

                // 移除区域中的所有视图，使其变空
                IoC.Get<INavigationService>().GoBack(RegionNames.ToolBoxRegion);
            }
            else
            {

                regionManager.RequestNavigate("ToolBoxRegion", nameof(NumberKeyPadView));
            }
            DirectChecked = false;
        }
        void BtnDirect() {

            if (!DirectChecked)
            {
                // 移除区域中的所有视图，使其变空
                IoC.Get<INavigationService>().GoBack(RegionNames.ToolBoxRegion);
            }
            else
            {
                IoC.Get<INavigationService>().NavigateToAsync(nameof(DirectView),RegionNames.ToolBoxRegion);

            }
            NumberKeyPadChecked = false;
        }

    }
}
