
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;

namespace PrismUI.Core
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

        public static string HeaderViewRegion => nameof(HeaderViewRegion);
    }

    public static class ViewNames
    {
        public static string NumberKeyPadView => nameof(NumberKeyPadView);
        public static string StringKeyPadView => nameof(StringKeyPadView);
        public static string AxisView => nameof(AxisView);
        public static string DirectView => nameof(DirectView);
        public static string MainView => nameof(MainView);

        public const string DefaultView = nameof(DefaultView);
        public const string EmptyView = nameof(EmptyView);
        public const string EmptyView2 = nameof(EmptyView2);
    }
    public interface INavigationService
    {
        Task NavigateToAsync(string viewName, string? regionName = null, NavigationParameters? parameters = null);
        Task<bool> ConfirmNavigationAsync();
        void GoBack(string? regionName = null);
        void GoForward(string? regionName = null);
    }

    public class NavigationService : INavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly  IEventAggregator _eventAggregator;

        public NavigationService(IRegionManager regionManager,IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// 导航到指定视图，如果存在 [ToolBoxFor] 配对，则同步导航工具栏区域。
        /// </summary>
        public Task NavigateToAsync(string viewName, string? regionName = null, NavigationParameters? parameters = null)
        {
            regionName ??= RegionNames.MainRegion;
            var tcs = new TaskCompletionSource<bool>();

            _regionManager.RequestNavigate(regionName, viewName, result =>
            {
                if (result.Success)
                {
                    if (regionName == RegionNames.MainRegion)
                    {
                        ClearToolBoxRegion();
                    }
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
            return true;
        }

        /// <summary>
        /// 后退：主区域和配对的工具栏区域同步后退。
        /// </summary>
        public void GoBack(string? regionName = null)
        {
            regionName ??= RegionNames.MainRegion;

            // 主区域后退
            var journal = GetJournal(regionName);
            if (journal?.CanGoBack == true)
            {
                if (regionName == RegionNames.MainRegion)
                {
                    ClearToolBoxRegion();
                }
                journal.GoBack();
               
            }
   
            // 配对的工具栏区域同步后退
            //if (PairedRegions.TryGetValue(regionName, out var toolBoxRegion))
            //{
            //    var toolBoxJournal = GetJournal(toolBoxRegion);
            //    if (toolBoxJournal?.CanGoBack == true)
            //    {

            //        toolBoxJournal.GoBack();
            //    }
            //}
        }

        /// <summary>
        /// 前进：主区域和配对的工具栏区域同步前进。
        /// </summary>
        public void GoForward(string? regionName = null)
        {
            regionName ??= RegionNames.MainRegion;

            // 主区域前进
            var journal = GetJournal(regionName);
            if (journal?.CanGoForward == true)
            {
                if (regionName == RegionNames.MainRegion)
                {
                    ClearToolBoxRegion();
                }
                journal.GoForward();
            }
           
            // 配对的工具栏区域同步前进
            //if (PairedRegions.TryGetValue(regionName, out var toolBoxRegion))
            //{
            //    var toolBoxJournal = GetJournal(toolBoxRegion);
            //    if (toolBoxJournal?.CanGoForward == true)
            //    {             
            //        toolBoxJournal.GoForward();
            //    }
            //}
        }

        private void ClearToolBoxRegion()
        {
            _eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Publish(true);
            if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ToolBoxRegion))
            {
                _eventAggregator.GetEvent<ToolBoxVisibilityChangedEvent>().Publish(false);
            }
        }

        private IRegionNavigationJournal? GetJournal(string? regionName = null)
        {
            regionName ??= RegionNames.MainRegion;
            if (!_regionManager.Regions.ContainsRegionWithName(regionName))
                return null;
            var region = _regionManager.Regions[regionName];
            return region?.NavigationService?.Journal;
        }

    }

}
