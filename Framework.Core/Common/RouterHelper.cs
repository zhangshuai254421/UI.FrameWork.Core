
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static string MainView => nameof(MainView);
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
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        // ToolBox 配对映射：MainViewName -> ToolBoxViewName（从 [ToolBoxFor] 特性自动扫描）
        private static Dictionary<string, string>? _toolBoxPairings;

        // 主区域和工具栏区域的配对关系：主Region -> 工具Region
        private static readonly Dictionary<string, string> PairedRegions = new()
        {
            { RegionNames.BaseViewMainRegion, RegionNames.ToolBoxRegion }
        };

        public NavigationService(IRegionManager regionManager, IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// 导航到指定视图，如果存在 [ToolBoxFor] 配对，则同步导航工具栏区域。
        /// </summary>
        public Task NavigateToAsync(string viewName, string? regionName = null, NavigationParameters? parameters = null)
        {
            regionName ??= RegionNames.BaseViewMainRegion;
            var tcs = new TaskCompletionSource<bool>();

            _regionManager.RequestNavigate(regionName, viewName, result =>
            {
                if (result.Success)
                {
                    // 检查是否有配对的工具栏视图需要联动导航
                    if (PairedRegions.TryGetValue(regionName, out var toolBoxRegion))
                    {
                        var pairedView = FindPairedToolBoxView(viewName);
                        if (pairedView != null)
                        {
                            _eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Publish(true); 
                            _regionManager.RequestNavigate(toolBoxRegion, pairedView);
                        }
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
            regionName ??= RegionNames.BaseViewMainRegion;

            // 主区域后退
            var journal = GetJournal(regionName);
            if (journal?.CanGoBack == true)
            {
                if (regionName == RegionNames.BaseViewMainRegion)
                {
                    _eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Publish(true);
                }
                journal.GoBack();
               
            }
   
            // 配对的工具栏区域同步后退
            if (PairedRegions.TryGetValue(regionName, out var toolBoxRegion))
            {
                var toolBoxJournal = GetJournal(toolBoxRegion);
                if (toolBoxJournal?.CanGoBack == true)
                {

                    toolBoxJournal.GoBack();
                }
            }
        }

        /// <summary>
        /// 前进：主区域和配对的工具栏区域同步前进。
        /// </summary>
        public void GoForward(string? regionName = null)
        {
            regionName ??= RegionNames.BaseViewMainRegion;

            // 主区域前进
            var journal = GetJournal(regionName);
            if (journal?.CanGoForward == true)
            {
                if (regionName == RegionNames.BaseViewMainRegion)
                {
                    _eventAggregator.GetEvent<FooterBtnIsCheckedChangedEvent>().Publish(true);
                }
                journal.GoForward();
            }
           
            // 配对的工具栏区域同步前进
            if (PairedRegions.TryGetValue(regionName, out var toolBoxRegion))
            {
                var toolBoxJournal = GetJournal(toolBoxRegion);
                if (toolBoxJournal?.CanGoForward == true)
                {             
                    toolBoxJournal.GoForward();
                }
            }
        }

        private IRegionNavigationJournal? GetJournal(string? regionName = null)
        {
            regionName ??= RegionNames.BaseViewMainRegion;
            if (!_regionManager.Regions.ContainsRegionWithName(regionName))
                return null;
            var region = _regionManager.Regions[regionName];
            return region?.NavigationService?.Journal;
        }

        /// <summary>
        /// 查找与指定 MainView 配对的 ToolBox 视图名称（通过 [ToolBoxFor] 特性）。
        /// </summary>
        private static string? FindPairedToolBoxView(string mainViewName)
        {
            EnsureToolBoxPairingsScanned();
            return _toolBoxPairings!.TryGetValue(mainViewName, out var toolBoxView) ? toolBoxView : null;
        }

        /// <summary>
        /// 懒加载扫描所有已加载程序集中的 [ToolBoxFor] 特性，建立配对映射。
        /// </summary>
        private static void EnsureToolBoxPairingsScanned()
        {
            if (_toolBoxPairings != null) return;

            _toolBoxPairings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 跳过系统程序集
                if (assembly.IsDynamic) continue;
                var name = assembly.GetName().Name;
                if (name != null && (name.StartsWith("System") || name.StartsWith("Microsoft") || name.StartsWith("mscorlib")))
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }
                catch { continue; }

                foreach (var type in types)
                {
                    var attrs = type.GetCustomAttributes(typeof(ToolBoxForAttribute), false);
                    foreach (var attr in attrs)
                    {
                        var toolBoxFor = (ToolBoxForAttribute)attr;
                        var mainViewName = toolBoxFor.MainViewType.Name;
                        var toolBoxViewName = type.Name;

                        // 同一个 MainView 可以有多个 ToolBox，这里取第一个
                        // 如果需要多个，可以改为 Dictionary<string, List<string>>
                        _toolBoxPairings.TryAdd(mainViewName, toolBoxViewName);
                    }
                }
            }
        }
    }

}
