using System.Threading;
using Framework.Alarm;
using Framework.Alarm.Domain;
using Prism.Commands;
using Prism.Navigation.Regions;
using PrismUI.Core;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：报警测试页 ViewModel（#15）——真引擎演示装置（AlarmDemoRig）喂 AlarmListViewModel，
    //       后台循环每秒推一步剧本（条件事实进引擎 + 用当前时刻重切两清单），验证报警模块全链路。
    //       列表零判定、事件编队、按期限过滤全在 AlarmListViewModel/AlarmFilters，本类只做调度。
    //       层级：主页→测试→报警测试（第 3 级，ADR-0005）。
    public class AlarmTestViewModel : BaseViewModel, INavigationAware
    {
        /// <summary>演示装置：真引擎 + 三条演示档案 + 循环剧本（#22 起接入真库）。</summary>
        private readonly AlarmDemoRig _rig;

        /// <summary>UI 上下文：清单重切与状态文本编队回 UI 线程（循环在后台，界面动作仍归 UI）。</summary>
        private readonly SynchronizationContext _ui;

        /// <summary>后台循环的取消源：泵循环的生命开关（null = 没在运行）。</summary>
        private CancellationTokenSource? _pumpCts;

        /// <summary>循环代数：每次开始自增。每步出手前核对代数，暂停或换代之后的尾步不再出手（#29 评审）。</summary>
        private int _pumpGen;

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

        /// <summary>停止模拟命令（暂停剧本；已有报警与搁置期限原样保留）。</summary>
        public DelegateCommand StopCommand { get; }

        /// <summary>
        /// 构造（#22 接入真库）：两个存储由容器注入（宿主 AddAlarmServices 的单例，库由宿主建好），
        /// 装置走真库；把真引擎接上列表 VM（编队到 UI 线程），泵循环跑在后台（#29）。
        /// </summary>
        public AlarmTestViewModel(IAlarmDefinitionStore definitions, IEventStore events)
        {
            _rig = new AlarmDemoRig(definitions, events);
            // 事件编队接 UI 线程（摘要 §9.2 选型定案：SynchronizationContext.Post）——
            // VM 由 Prism 在 UI 线程创建，Current 即派发器上下文；取不到时再造一个（设计器等无上下文场合）。
            _ui = SynchronizationContext.Current
                ?? new System.Windows.Threading.DispatcherSynchronizationContext(
                    System.Windows.Threading.Dispatcher.CurrentDispatcher);
            List = new AlarmListViewModel(_rig.Engine, _rig.Store, _ui);

            StartCommand = new DelegateCommand(Start);
            StopCommand = new DelegateCommand(() => Stop());
        }

        /// <summary>开始模拟：后台循环跑起来——条件事实进引擎与落库都在后台线程（#29 的病根在此）：
        /// 库被外部锁住（备份/杀毒）时，busy 等待冻住的是循环不是屏幕，UI 侧只收编队回来的事件与清单。</summary>
        private void Start()
        {
            if (_pumpCts is not null)
            {
                return; // 已在运行：与 DispatcherTimer.Start 的重复开始同义，不动
            }

            _pumpCts = new CancellationTokenSource();
            var token = _pumpCts.Token;
            var gen = ++_pumpGen;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token); // 每秒一步（先等再推，与原 DispatcherTimer 同拍点）
                    }
                    catch (TaskCanceledException)
                    {
                        return; // 停止模拟：正常收场
                    }

                    // 出手前核对代数（#29 评审）：暂停/换代后已在途的这一步不再出手——
                    // 否则库忙拖住一步时，状态栏已报「已暂停」，新报警却姗姗落地。
                    if (token.IsCancellationRequested || gen != _pumpGen)
                    {
                        return;
                    }

                    try
                    {
                        _rig.PumpNext();                        // 剧本下一步：条件事实进真引擎（后台执行，事件由列表 VM 编队回 UI）
                        _ui.Post(_ => List.RefreshFilters(), null); // 用当前时刻重切两清单（集合动作归 UI 线程）：搁置到期在这里自然回主列表
                    }
                    catch (Exception ex)
                    {
                        _ui.Post(_ => Stop($"模拟故障停演：{ex.Message}"), null); // 故障说出口（#29 评审）：别装成人为暂停，否则现场没处查
                        return;
                    }
                }
            });

            StatusText = "模拟中：升温/憋压/液位上涨 → 保持 → 全体恢复，循环投喂真引擎";
        }

        /// <summary>暂停：循环停下，现场（报警与搁置期限）原样保留；<paramref name="reason"/> 非空时状态栏如实报因。</summary>
        private void Stop(string? reason = null)
        {
            _pumpCts?.Cancel();
            _pumpCts = null;
            _pumpGen++;                                     // 立即作废在途步：暂停后不落尾步（#29 评审）
            StatusText = reason ?? "模拟已暂停";
        }

        #region INavigationAware

        /// <summary>进入页面：先核对活跃快照（事件流断档时的兜底），再开始模拟。</summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            List.Reload();
            Start();
        }

        /// <summary>始终复用当前实例：装置与列表随页面复用。</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>离开页面：停止模拟（不销毁装置——回来时接着用）。</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Stop();
        }

        #endregion
    }
}
