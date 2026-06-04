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
using UI.FrameWork.Core.Main.Footer;

namespace UI.FrameWork.Core.Main
{
    public class FooterViewModel : BindableBase
    {

        private readonly IRegionManager _regionManager;
        private readonly PrismRegionNavigator _regionNavigator;
        private readonly DelegateCommand _btnNumberKeyPadCommand = null!;
        private readonly DelegateCommand _btnDirectCommand = null!;

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

        private bool logShowChecked;
        public bool LogShowChecked
        {
            get => logShowChecked;
            set => SetProperty(ref logShowChecked, value);
        }



        public FooterViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            _regionManager = regionManager;
            _regionNavigator = regionNavigator;
            eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Subscribe(isChecked =>
            {
                if (DirectChecked|| NumberKeyPadChecked||logShowChecked) {
                    IoC.Get<INavigationService>().NavigateToAsync(ViewNames.EmptyView2, RegionNames.ToolBoxRegion);
                }
                DirectChecked = isChecked ? false : true;
                NumberKeyPadChecked = isChecked ? false : true;
                LogShowChecked = isChecked ? false : true;
            });
        }

        public DelegateCommand BtnNumberKeyPadCommand => _btnNumberKeyPadCommand ??new DelegateCommand(BtnNumberKeyPad);
        public DelegateCommand BtnDirectCommand => _btnDirectCommand ?? new DelegateCommand(BtnDirect);

        private DelegateCommand _btnLogShowCommand = null!;
        public DelegateCommand BtnLogShowCommand=> _btnLogShowCommand?? new DelegateCommand(() =>
           {
               if (!LogShowChecked)
               {
                   // 移除区域中的所有视图，使其变空
                   IoC.Get<INavigationService>().NavigateToAsync(ViewNames.EmptyView2, RegionNames.ToolBoxRegion);
               }
               else
               {
                   regionManager.RequestNavigate("ToolBoxRegion", nameof(LogShowView));
               }
               
           });

        void BtnNumberKeyPad()
        {
 
           if (!NumberKeyPadChecked)
            {

                // 移除区域中的所有视图，使其变空
                IoC.Get<INavigationService>().NavigateToAsync(ViewNames.EmptyView2, RegionNames.ToolBoxRegion);
            }
            else
            {

                regionManager.RequestNavigate("ToolBoxRegion", nameof(NumberKeyPadView));
            }
            DirectChecked = false;
            LogShowChecked = false;
        }
        void BtnDirect() {

            if (!DirectChecked)
            {
                // 移除区域中的所有视图，使其变空
                IoC.Get<INavigationService>().NavigateToAsync(ViewNames.EmptyView2, RegionNames.ToolBoxRegion);
            }
            else
            {
                IoC.Get<INavigationService>().NavigateToAsync(nameof(DirectView),RegionNames.ToolBoxRegion);

            }
            NumberKeyPadChecked = false;
            LogShowChecked = false;
        }

    }
}
