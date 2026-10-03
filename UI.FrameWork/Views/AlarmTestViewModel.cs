using System.Threading;
using System.Windows.Threading;
using Framework.Alarm;
using Prism.Commands;
using Prism.Navigation.Regions;
using PrismUI.Core;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：报警测试页 ViewModel（#15）——真引擎演示装置（AlarmDemoRig）喂 AlarmListViewModel，
    //       DispatcherTimer 每秒推一拍剧本（条件事实进引擎 + 对表重切两清单），验证报警模块全链路。
    //       列表零判定、事件编队、按标过滤全在 AlarmListViewModel/AlarmFilters，本类只是舞台监督。
    //       层级：主页→测试→报警测试（第 3 级，ADR-0005）。
    public class AlarmTestViewModel : BaseViewModel, INavigationAware
    {
        /// <summary>演示装置：真引擎 + 三条演示档案 + 循环剧本。</summary>
        private readonly AlarmDemoRig _rig = new();

        /// <summary>剧本时钟：每拍 1 秒推进剧本并重切两清单（搁置到期靠这一拍对表自然回主列表）。</summary>
        private readonly DispatcherTimer _timer;

        /// <summary>列表 VM：主列表/搁置清单/详情的数据源（页面绑定都打在 List.* 上）。</summary>
        public AlarmListViewModel List { get; }

        /// <summary>状态文本的后端字段。</summary>
        private string _statusText = "模拟未开始";

        /// <summary>状态提示：剧本播到哪了。</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>开始模拟命令。</summary>
        public DelegateCommand StartCommand { get; }

        /// <summary>停止模拟命令（暂停剧本；已有报警与搁置标原样保留）。</summary>
        public DelegateCommand StopCommand { get; }

        /// <summary>构造：把真引擎接上列表 VM（编队到 UI 线程），装好每秒一拍的剧本时钟。</summary>
        public AlarmTestViewModel()
        {
            // 事件编队接 UI 线程（摘要 §9.2 选型定案：SynchronizationContext.Post）——
            // VM 由 Prism 在 UI 线程创建，Current 即派发器上下文；兜底再造一个（设计器等无上下文场合）。
            var ui = SynchronizationContext.Current
                ?? new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
            List = new AlarmListViewModel(_rig.Engine, _rig.Store, ui);

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            _timer.Tick += (_, _) =>
            {
                _rig.PumpNext();        // 剧本下一拍：条件事实进真引擎（事件同步吐出、Post 编队回 UI）
                List.RefreshFilters();  // 对表重切两清单：搁置到期在这里自然回主列表，无需解除
            };

            StartCommand = new DelegateCommand(Start);
            StopCommand = new DelegateCommand(Stop);
        }

        /// <summary>开演：时钟走起来。</summary>
        private void Start()
        {
            _timer.Start();
            StatusText = "模拟中：升温/憋压/液位上涨 → 保持 → 全体恢复，循环投喂真引擎";
        }

        /// <summary>暂停：时钟停下，现场（报警与搁置标）原样保留。</summary>
        private void Stop()
        {
            _timer.Stop();
            StatusText = "模拟已暂停";
        }

        #region INavigationAware

        /// <summary>进入页面：活跃快照对账（事件流断档的兜底）后开演。</summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            List.Reload();
            Start();
        }

        /// <summary>始终复用当前实例：装置与列表随页面复用。</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>离开页面：停演（不撒手装置——回来的戏还要接着唱）。</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Stop();
        }

        #endregion
    }
}
