using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using PrismUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UI.FrameWork.Core.Common;
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
        private readonly  IEventAggregator eventAggregator;
        private readonly INavigationService _navigationService;


        public FooterViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator, IEventAggregator eventAggregator, INavigationService navigationService)
        {
            this.regionManager = regionManager;
            _regionManager = regionManager;
            _regionNavigator = regionNavigator;
            this.eventAggregator = eventAggregator;
            _navigationService = navigationService;
            this.eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Subscribe(isChecked =>
            {
                DirectChecked = isChecked ? false : true;
                NumberKeyPadChecked = isChecked ? false : true;
                LogShowChecked = isChecked ? false : true;
            });
            
        }

        public DelegateCommand BtnNumberKeyPadCommand => _btnNumberKeyPadCommand ?? new DelegateCommand(BtnNumberKeyPad);
        public DelegateCommand BtnDirectCommand => _btnDirectCommand ?? new DelegateCommand(BtnDirect);

        private DelegateCommand _btnLogShowCommand = null!;
        public DelegateCommand BtnLogShowCommand => _btnLogShowCommand ?? new DelegateCommand(() =>
        {
            if (LogShowChecked)
            {
                regionManager.RequestNavigate("ToolBoxRegion", nameof(LogShowView));
            }
            eventAggregator.GetEvent<ToolBoxVisibilityChangedEvent>().Publish(LogShowChecked);
            DirectChecked = false;
            NumberKeyPadChecked = false;
        });

        void BtnNumberKeyPad()
        {
            if (NumberKeyPadChecked)
            {
                regionManager.RequestNavigate("ToolBoxRegion", nameof(NumberKeyPadView));
            }
            eventAggregator.GetEvent<ToolBoxVisibilityChangedEvent>().Publish(NumberKeyPadChecked);
            DirectChecked = false;
            LogShowChecked = false;
        }

        void BtnDirect()
        {
            if (DirectChecked)
            {
                _navigationService.NavigateToAsync(nameof(DirectView), RegionNames.ToolBoxRegion);
            }
            eventAggregator.GetEvent<ToolBoxVisibilityChangedEvent>().Publish(DirectChecked);
            NumberKeyPadChecked = false;
            LogShowChecked = false;
        }

    }
}
