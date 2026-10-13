using Prism.Commands;
using Prism.Events;
using PrismUI.Core;
using Recipe.Infrastructure.Contracts;
using System;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main
{
    /// <summary>
    /// 页头 ViewModel：展示"配方组/配方名"标题，并在切换配方事件到达时自动刷新。
    /// 另持有顶栏报警按钮的中性三件套（级别/计数/文本）：壳不认识报警家族，数值由家族桥 AlarmHeaderBridge 写入。
    /// </summary>
    public class HeaderViewModel : BindableBase
    {
        #region 字段

        /// <summary>配方服务，用于拉取当前配方信息刷新标题。</summary>
        private readonly IRecipeService _recipeService;
        /// <summary>事件聚合器，订阅切换配方事件。</summary>
        private readonly IEventAggregator _eventAggregator;
        /// <summary>导航服务：顶栏报警按钮跳转报警列表页（按 ViewNames 名字导航，不引用家族程序集）。</summary>
        private readonly INavigationService _navigationService;
        /// <summary>页头标题，初始为默认占位文本。</summary>
        private string _title = "Default Title";
        /// <summary>顶栏报警级别（默认无未闭环报警）。</summary>
        private HeaderAlarmLevel _alarmLevel = HeaderAlarmLevel.Normal;
        /// <summary>顶栏报警计数：未搁置的活跃报警行数。</summary>
        private int _alarmCount;
        /// <summary>顶栏报警文本：最高优先级那条的位号。</summary>
        private string _alarmText = string.Empty;
        /// <summary>是否显示顶栏报警按钮：报警家族模块装载时才置真，壳默认不亮。</summary>
        private bool _isShowAlarm;

        #endregion

        #region 构造函数

        /// <summary>构造：注入服务并异步拉取标题，同时订阅切换配方事件以触发刷新。</summary>
        public HeaderViewModel(IRecipeService recipeService, IEventAggregator eventAggregator, INavigationService navigationService)
        {
            _eventAggregator = eventAggregator;
            _recipeService = recipeService;
            _navigationService = navigationService;
            _ = UpdateTitleAsync();

            _eventAggregator.GetEvent<ChangeRecipeEvent>().Subscribe((recipeName) =>
            {
                _ = UpdateTitleAsync();
            });

            ShowAlarmsCommand = new DelegateCommand(ShowAlarms);
        }

        #endregion

        #region 属性

        /// <summary>页头标题，绑定页头视图文本，格式为"配方组/配方名"。</summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>顶栏报警级别：家族桥按活跃快照写入（规则在家族侧 AlarmHeaderStatus）。</summary>
        public HeaderAlarmLevel AlarmLevel
        {
            get => _alarmLevel;
            set => SetProperty(ref _alarmLevel, value);
        }

        /// <summary>顶栏报警计数：未搁置的活跃报警行数。</summary>
        public int AlarmCount
        {
            get => _alarmCount;
            set => SetProperty(ref _alarmCount, value);
        }

        /// <summary>顶栏报警文本：最高优先级那条的位号。</summary>
        public string AlarmText
        {
            get => _alarmText;
            set => SetProperty(ref _alarmText, value);
        }

        /// <summary>是否显示顶栏报警按钮：家族模块装载时由 AlarmUIModule 置真。</summary>
        public bool IsShowAlarm
        {
            get => _isShowAlarm;
            set => SetProperty(ref _isShowAlarm, value);
        }

        #endregion

        #region 命令

        /// <summary>顶栏报警按钮：跳转报警列表页（家族模块注册 ViewNames.AlarmListView，壳不引用家族程序集）。</summary>
        public DelegateCommand ShowAlarmsCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 从 service 拉当前配方 DTO 刷新标题（ADR 0002：VM 不碰实体、不同步阻塞）。
        /// 标题是纯展示，加载失败时保持现状即可。
        /// </summary>
        private async Task UpdateTitleAsync()
        {
            try
            {
                var current = await _recipeService.GetCurrentRecipeAsync();
                Title = $"{current?.GroupName}/{current?.RecipeName}";
            }
            catch (Exception)
            {
                // 保持现状
            }
        }

        /// <summary>跳转报警列表页；家族模块没装时按钮整个隐藏，这条命令不会被点到。</summary>
        private void ShowAlarms()
        {
            _ = _navigationService.NavigateToAsync(ViewNames.AlarmListView, RegionNames.MainRegion);
        }

        #endregion
    }
}
