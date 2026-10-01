
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;

namespace PrismUI.Core
{
    /// <summary>
    /// 导航服务抽象：封装 Prism 区域导航，支持异步导航与前进/后退。
    /// </summary>
    public interface INavigationService
    {
        /// <summary>异步导航到指定视图；regionName 为空时导航到主区域，失败时任务携带 false 或异常。</summary>
        Task NavigateToAsync(string viewName, string? regionName = null, NavigationParameters? parameters = null);
        /// <summary>确认是否允许离开当前导航；当前实现始终返回 true。</summary>
        Task<bool> ConfirmNavigationAsync();
        /// <summary>后退到上一视图；regionName 为空时作用于主区域，不可后退时静默忽略。</summary>
        void GoBack(string? regionName = null);
        /// <summary>前进到下一视图；regionName 为空时作用于主区域，不可前进时静默忽略。</summary>
        void GoForward(string? regionName = null);
    }

    /// <summary>
    /// 基于 Prism 区域管理器的导航服务实现：主区域导航时同步清理工具箱区域。
    /// </summary>
    public class NavigationService : INavigationService
    {
        /// <summary>Prism 区域管理器，用于发起区域导航。</summary>
        private readonly IRegionManager _regionManager;
        /// <summary>事件聚合器，用于发布工具箱显隐相关事件。</summary>
        private readonly  IEventAggregator _eventAggregator;

        /// <summary>构造函数：注入区域管理器与事件聚合器。</summary>
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

        /// <summary>确认是否允许离开当前导航；当前始终返回 true。</summary>
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

        /// <summary>清空工具箱区域：先取消页脚全屏勾选，再发布工具箱隐藏事件。</summary>
        private void ClearToolBoxRegion()
        {
            _eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Publish(true);
            if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ToolBoxRegion))
            {
                _eventAggregator.GetEvent<ToolBoxVisibilityChangedEvent>().Publish(false);
            }
        }

        /// <summary>获取指定区域的导航日志；区域不存在时返回 null，regionName 为空时取主区域。</summary>
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
