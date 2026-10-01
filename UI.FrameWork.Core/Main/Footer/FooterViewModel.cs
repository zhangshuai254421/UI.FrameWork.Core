              
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
    /// <summary>
    /// 页脚工具栏 ViewModel：维护数字键盘、直连视图、日志展示三个互斥的开关按钮，
    /// 选中项对应的视图被导航到工具箱区域。
    /// </summary>
    public class FooterViewModel : BindableBase
    {

        /// <summary>Prism 区域管理器（预留，未直接使用）。</summary>
        private readonly IRegionManager _regionManager;
        /// <summary>区域导航器（预留，未直接使用）。</summary>
        private readonly PrismRegionNavigator _regionNavigator;
        /// <summary>数字键盘按钮命令的缓存实例。</summary>
        private readonly DelegateCommand _btnNumberKeyPadCommand = null!;
        /// <summary>直连视图按钮命令的缓存实例。</summary>
        private readonly DelegateCommand _btnDirectCommand = null!;

        /// <summary>区域管理器，用于向工具箱区域发起导航。</summary>
        private readonly IRegionManager regionManager;
        /// <summary>数字键盘按钮的选中状态。</summary>
        private bool numberKeyPadChecked;

        /// <summary>数字键盘按钮选中状态，绑定页脚视图的开关按钮。</summary>
        public bool NumberKeyPadChecked
        {
            get => numberKeyPadChecked;
            set => SetProperty(ref numberKeyPadChecked, value);
        }

  
        /// <summary>直连视图按钮的选中状态。</summary>
        private bool directChecked;

        /// <summary>直连视图按钮选中状态，绑定页脚视图的开关按钮。</summary>
        public bool DirectChecked
        {
            get => directChecked;
            set => SetProperty(ref directChecked, value);
        }

        /// <summary>日志展示按钮的选中状态。</summary>
        private bool logShowChecked;
        /// <summary>日志展示按钮选中状态，绑定页脚视图的开关按钮。</summary>
        public bool LogShowChecked
        {
            get => logShowChecked;
            set => SetProperty(ref logShowChecked, value);
        }
        /// <summary>事件聚合器：订阅按钮互斥选中事件，发布工具箱可见性变化。</summary>
        private readonly  IEventAggregator eventAggregator;
        /// <summary>应用级导航服务，用于向指定区域跳转。</summary>
        private readonly INavigationService _navigationService;


        /// <summary>注入区域与事件服务，并订阅互斥选中事件：任一工具箱按钮被选中时，其余按钮自动取消选中。</summary>
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

        /// <summary>数字键盘按钮命令：选中时把数字键盘视图导航到工具箱区域，并取消其余按钮选中。</summary>
        public DelegateCommand BtnNumberKeyPadCommand => _btnNumberKeyPadCommand ?? new DelegateCommand(BtnNumberKeyPad);
        /// <summary>直连视图按钮命令：选中时把直连视图导航到工具箱区域，并取消其余按钮选中。</summary>
        public DelegateCommand BtnDirectCommand => _btnDirectCommand ?? new DelegateCommand(BtnDirect);

        /// <summary>日志展示按钮命令的缓存实例。</summary>
        private DelegateCommand _btnLogShowCommand = null!;
        /// <summary>日志展示按钮命令：选中时把日志展示视图导航到工具箱区域，并取消其余按钮选中。</summary>
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

        /// <summary>数字键盘按钮点击处理：选中时导航到工具箱区域，随后发布可见性变化并取消其余按钮选中。</summary>
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

        /// <summary>直连视图按钮点击处理：选中时导航到工具箱区域，随后发布可见性变化并取消其余按钮选中。</summary>
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
