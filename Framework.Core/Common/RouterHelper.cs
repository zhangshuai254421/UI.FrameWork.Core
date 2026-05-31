
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.Common
{
    public static class RegionNames
    {
        #region ShellWindow
        /// <summary>
        /// 主页面
        /// </summary>
        public static string MainRegion => nameof(MainRegion);

        /// <summary>
        /// 下方的工具栏区域
        /// </summary>
        public static string ToolBoxRegion => nameof(ToolBoxRegion);

        /// <summary>
        /// 右下角的页脚区域
        /// </summary>
        public static string FooterRegion => nameof(FooterRegion);
        #endregion

        /// <summary>
        /// 主页面的主区域（BaseView 内部的 Region）
        /// </summary>
        public static string BaseViewMainRegion= nameof(BaseViewMainRegion);

        public static string HeaderViewRegion => nameof(HeaderViewRegion);
    }

    public static class ViewNames
    {
        public static string NumberKeyPadView => nameof(NumberKeyPadView);
        public static string StringKeyPadView => nameof(StringKeyPadView);
        public static string AxisView => nameof(AxisView);
        public static string DirectView => nameof(DirectView);
    }
    public interface INavigationService
    {
        Task NavigateToAsync(string viewName, NavigationParameters parameters = null);
        Task<bool> ConfirmNavigationAsync();
        void GoBack();
        void GoForward();
    }

    public class NavigationService : INavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;

        public NavigationService(IRegionManager regionManager, IDialogService dialogService)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
        }

        public Task NavigateToAsync(string viewName, NavigationParameters parameters = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            _regionManager.RequestNavigate(RegionNames.MainRegion, viewName, result =>
            {
                if (result.Success)
                {
                    tcs.SetResult(true);
                }
                else if (result.Exception != null)
                {
                    tcs.SetException(result.Exception);
                }
                else
                {
                    tcs.SetResult(false);
                }
            }, parameters);

            return tcs.Task;
        }

        public async Task<bool> ConfirmNavigationAsync()
        {
            //return await _dialogService.ShowDialogAsync("确认, 是否继续导航？").Result.Result == ButtonResult.OK;
            return true;
        }

        public void GoBack()
        {
            var journal = GetJournal();
            if (journal?.CanGoBack == true)
            {
                journal.GoBack();
            }
        }

        public void GoForward()
        {
            var journal = GetJournal();
            if (journal?.CanGoForward == true)
            {
                journal.GoForward();
            }
        }

        private IRegionNavigationJournal GetJournal()
        {
            var region = _regionManager.Regions[RegionNames.MainRegion];
            return region?.NavigationService?.Journal;
        }
    }

}
