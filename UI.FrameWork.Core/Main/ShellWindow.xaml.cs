using Framework.Core.Common;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrismUI.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace UI.FrameWork.Core.Main
{
    /// <summary>
    /// ShellWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShellWindow : Window, INotifyPropertyChanged
    {


        public ShellWindow()
        {
            InitializeComponent();
            DataContext = this;

            var chrome = new WindowChrome()
            {
                CaptionHeight = 0,
                UseAeroCaptionButtons = false,
                CornerRadius = new CornerRadius(),
                GlassFrameThickness = new Thickness(0, 0, 0, 1)
            };
            WindowChrome.SetWindowChrome(this, chrome);
            WindowStyle = WindowStyle.None;
            //WindowState = WindowState.Maximized;

            Width = 1280;
            Height = 1024;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        // CallerMemberName 会自动填充调用该方法的属性名称
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        private Visibility _isToolBoxVisible = Visibility.Collapsed;

        public Visibility IsToolBoxVisible
        {
            get => _isToolBoxVisible;
            set
            {
                if (_isToolBoxVisible != value)
                {
                    _isToolBoxVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        

        private void BtnNavigateBackCommand(object sender, RoutedEventArgs e)
        {
           
            //var s = IoC.Get<INavigationService>();
            IoC.Get<INavigationService>().GoBack();
        }

        private void BtnNavigateForwarddCommand(object sender, RoutedEventArgs e)
        {
            IoC.Get<INavigationService>().GoForward();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //// 订听工具箱显隐事件
            IoC.Get<IEventAggregator>()
               .GetEvent<ToolBoxVisibilityChangedEvent>()
               .Subscribe(visible => IsToolBoxVisible = visible ? Visibility.Visible : Visibility.Collapsed);
        }
    }
}
