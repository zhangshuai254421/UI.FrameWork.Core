using Framework.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class ShellWindow : Window
    {


        public ShellWindow()
        {
            InitializeComponent();

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
        //private readonly DelegateCommand _btnNavigateBackCommand = null!;
        //private readonly DelegateCommand _btnNavigateForwardCommand = null!;
        //public DelegateCommand BtnNavigateBackCommand => _btnNavigateBackCommand ?? new DelegateCommand(() =>
        //{

        //    IoC.Get<INavigationService>().GoBack();
        //});

        //public DelegateCommand BtnNavigateForwarddCommand => _btnNavigateForwardCommand ?? new DelegateCommand(() =>
        //{

        //    IoC.Get<INavigationService>().GoForward();
        //});

        private void BtnNavigateBackCommand(object sender, RoutedEventArgs e)
        {
            IoC.Get<INavigationService>().GoBack();
        }

        private void BtnNavigateForwarddCommand(object sender, RoutedEventArgs e)
        {
            IoC.Get<INavigationService>().GoForward();
        }
    }
}
