using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UI.FrameWork.Core.Common;

namespace UI.FrameWork.Core.Main
{
    public class FooterViewModel : BindableBase
    {

        private readonly IRegionManager _regionManager;
        private readonly PrismRegionNavigator _regionNavigator;
        private  readonly DelegateCommand _btnNumberKeyPadCommand = null!;
        private readonly IRegionManager regionManager;
        private bool numberKeyPadChecked;

        public bool NumberKeyPadChecked
        {
            get => numberKeyPadChecked;
            set => SetProperty(ref numberKeyPadChecked, value);
        }

        private bool stringKeyPadChecked;

        public bool StringKeyPadChecked
        {
            get => stringKeyPadChecked;
            set => SetProperty(ref stringKeyPadChecked, value);
        }
        private bool axisChecked;

        public bool AxisChecked
        {
            get => axisChecked;
            set => SetProperty(ref axisChecked, value);
        }
        private bool directChecked;

        public bool DirectChecked
        {
            get => directChecked;
            set => SetProperty(ref directChecked, value);
        }


        public FooterViewModel(IRegionManager regionManager, PrismRegionNavigator regionNavigator)
        {
            this.regionManager = regionManager;
            _regionManager = regionManager;
            _regionNavigator = regionNavigator;
        }

        public DelegateCommand BtnNumberKeyPadCommand => _btnNumberKeyPadCommand ??new DelegateCommand(BtnNumberKeyPad);
        public DelegateCommand BtnDirectCommand => _btnNumberKeyPadCommand ?? new DelegateCommand(BtnDirect);

        void BtnNumberKeyPad()
        {
            // 获取目标区域
            IRegion targetRegion = _regionManager.Regions["ToolBoxRegion"];

            if (!NumberKeyPadChecked)
            {

                // 移除区域中的所有视图，使其变空
                foreach (var view in targetRegion.Views.ToList())
                {
                    targetRegion.Remove(view);
                }
            }
            else
            {

                regionManager.RequestNavigate("ToolBoxRegion", nameof(NumberKeyPadView));
                //_regionNavigator.NavigateAsync("ToolBoxRegion", "HeaderView");
            }
            StringKeyPadChecked = false;
            AxisChecked = false;
            DirectChecked = false;
        }
        void BtnDirect() {
            // 获取目标区域
            IRegion targetRegion = _regionManager.Regions["ToolBoxRegion"];

            if (!DirectChecked)
            {
                // 移除区域中的所有视图，使其变空
                foreach (var view in targetRegion.Views.ToList())
                {
                    targetRegion.Remove(view);
                }
            }
            else
            {

                regionManager.RequestNavigate("ToolBoxRegion", nameof(DirectView));
                //_regionNavigator.NavigateAsync("ToolBoxRegion", "HeaderView");
            }
            StringKeyPadChecked = false;
            AxisChecked = false;
            NumberKeyPadChecked = false;
        }

    }
}
