using System.Collections.ObjectModel;
using Framework.Alarm;
using Framework.Alarm.Domain;
using Prism.Commands;
using Prism.Mvvm;

namespace SemiAppliaction.Views
{
    /// <summary>
    /// 报警列表 ViewModel（摘要 §9.3，哲学 §3.2 呈现纪律【强制】）：事件 → 集合变更的直译，
    /// Apply 里零判定——Raised→Upsert、Acknowledged/AutoReturned→UpdateState、Closed→Remove、
    /// Shelved/Unshelved→UpdateMark。「RTN 留列表、Closed 移出、按标过滤」的规矩都是引擎定的，
    /// UI 照单全收；排序按档案 Priority（高优永远在顶），过滤调用引擎库的 <see cref="AlarmFilters"/>。
    /// 跨线程编队（摘要 §9.2 选型定案：SynchronizationContext.Post）：引擎在通讯线程吐事件，
    /// 本类把每一拍 Post 到注入的上下文再翻译；测试注入同步上下文即同步执行路径（不依赖真线程）。
    /// 只认 <see cref="IAlarmFeed"/> 接缝——测试 fake 一个 feed 即可红绿灯，不依赖真引擎。
    /// 行实例终生制：事件只原位刷行、清单原位对账（容器不重建，闪烁不重启、选中不丢）；
    /// 唯一例外是 <see cref="Reload"/> 整体重建——走完按位号重锚选中（#25），弹窗不绑死旧实例；
    /// 详情档案走后台预取（#31）：选行只是下单，档案由取档接缝在后台去库里拿，取回编队回 UI——
    /// 库被外部锁住（#29 场景）时 busy 等待冻的是后台线程，不是点了行的 UI；换行对暗号（代数，
    /// 同泵线规矩），慢档归位时选中已换人就作废不出手。取档炸了也停在取档处，不炸上 UI。
    /// <see cref="Dispose"/> 退订事件流——feed 长命（单例引擎）而页面短命时由宿主收口。
    /// </summary>
    public sealed class AlarmListViewModel : BindableBase, IDisposable
    {
        /// <summary>馈送接缝：事件流与操作员动作的唯一通道（UI 不认得引擎具体类）。</summary>
        private readonly IAlarmFeed _feed;

        /// <summary>档案接缝：详情弹窗的 MADB 数据源（档案原样呈现，UI 不加工）。</summary>
        private readonly IAlarmDefinitionStore _store;

        /// <summary>UI 上下文：事件编队到 UI 线程（摘要 §9.2）。</summary>
        private readonly SynchronizationContext _ui;

        /// <summary>读取时刻：喂给 AlarmFilters 对表（测试注入固定值拨表）。</summary>
        private readonly Func<DateTimeOffset> _now;

        /// <summary>取档接缝（#31）：下单在后台跑——产品默认 Task.Run（线程池），测试注入内联即同步路径。</summary>
        private readonly Action<Action> _runOffUiThread;

        /// <summary>取档代数（#31）：每回下单自增——慢档归位对暗号，选中已换人就作废不出手。</summary>
        private int _detailGen;

        /// <summary>行登记：位号 → 行（事件只更行，两清单由 Refill 重切）。</summary>
        private readonly Dictionary<string, AlarmRow> _rows = new();

        /// <summary>构造：订上馈送事件流（Post 编队到 UI 线程），初始对一次表。</summary>
        /// <param name="feed">馈送接缝：事件流与操作员动作的通道。</param>
        /// <param name="store">档案接缝：详情弹窗的数据源（只经取档接缝在后台碰）。</param>
        /// <param name="ui">UI 上下文：事件与取回的档案都编队回这里。</param>
        /// <param name="now">读取时刻：测试拨表用。</param>
        /// <param name="runOffUiThread">取档接缝（#31）：默认 Task.Run 后台线程；测试注内联即同步路径。</param>
        public AlarmListViewModel(IAlarmFeed feed, IAlarmDefinitionStore store, SynchronizationContext ui,
            Func<DateTimeOffset>? now = null, Action<Action>? runOffUiThread = null)
        {
            _feed = feed;
            _store = store;
            _ui = ui;
            _now = now ?? (() => DateTimeOffset.Now);
            _runOffUiThread = runOffUiThread ?? (fetch => Task.Run(fetch));
            _feed.EventRaised += OnEventRaised;

            AcknowledgeCommand = new DelegateCommand<AlarmRow>(Acknowledge);
            ShelveCommand = new DelegateCommand<AlarmRow>(Shelve);
            UnshelveCommand = new DelegateCommand<AlarmRow>(Unshelve);
            ShowDetailCommand = new DelegateCommand<AlarmRow>(ShowDetail);
            CloseDetailCommand = new DelegateCommand(() => SelectedRow = null);
            RefreshFilters();
        }

        /// <summary>订阅随页面生命周期：本 VM 随页面存亡；引擎 Dispose 后事件流自然哑火。</summary>
        private void OnEventRaised(AlarmEvent e) => _ui.Post(_ => Apply(e), null);

        #region 绑定集合与选中

        /// <summary>主列表：不带有效标（或标已过期）的成员——搁置中的报警按标滤出，正常播报。</summary>
        public ObservableCollection<AlarmRow> MainRows { get; } = [];

        /// <summary>搁置清单：带标且期限未过的成员——「搁置清单单独可见」在这一列兑现（哲学 §7）。</summary>
        public ObservableCollection<AlarmRow> ShelvedRows { get; } = [];

        /// <summary>选中行的后端字段。</summary>
        private AlarmRow? _selectedRow;

        /// <summary>选中行：详情弹窗的数据锚点；置行即下单取档案（后台，#31），置空即收起弹窗；
        /// 重按同一行也下单——上次取档没归（库忙/炸了）时，再点一次就是重新下单。</summary>
        public AlarmRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                var changed = SetProperty(ref _selectedRow, value);
                if (changed || value is not null)
                {
                    FetchDetail(value?.Tag);
                }
            }
        }

        /// <summary>详情档案的后端字段。</summary>
        private AlarmDefinition? _detail;

        /// <summary>详情弹窗的 MADB 档案（原样呈现；null = 弹窗收起）。</summary>
        public AlarmDefinition? Detail
        {
            get => _detail;
            private set => SetProperty(ref _detail, value);
        }

        /// <summary>操作员署名：动作转手 feed 时随行（演示页默认，宿主接登录态）。</summary>
        public string Operator { get; set; } = "演示员";

        /// <summary>搁置时长：UI 供给、必须限时（无限期搁置 = 变相删报警，哲学 §7）。</summary>
        public TimeSpan ShelveDuration { get; set; } = TimeSpan.FromSeconds(30);

        #endregion

        #region 命令（操作员动作只转手 feed，不自改状态）

        /// <summary>确认命令：转手确认，状态等引擎的 Acknowledged 事件回弹。</summary>
        public DelegateCommand<AlarmRow> AcknowledgeCommand { get; }

        /// <summary>搁置命令：转手搁置，时长取 ShelveDuration（UI 供给且必须限时）。</summary>
        public DelegateCommand<AlarmRow> ShelveCommand { get; }

        /// <summary>解除搁置命令：转手解除——搁置清单行上的「解除搁置」按钮。</summary>
        public DelegateCommand<AlarmRow> UnshelveCommand { get; }

        /// <summary>详情命令：选中该行并取档案（MADB 原样呈现）。</summary>
        public DelegateCommand<AlarmRow> ShowDetailCommand { get; }

        /// <summary>收起详情命令。</summary>
        public DelegateCommand CloseDetailCommand { get; }

        private void Acknowledge(AlarmRow? row)
        {
            if (row is not null)
            {
                _feed.Acknowledge(row.Tag, Operator);
            }
        }

        private void Shelve(AlarmRow? row)
        {
            if (row is not null)
            {
                _feed.Shelve(row.Tag, ShelveDuration, Operator);
            }
        }

        private void Unshelve(AlarmRow? row)
        {
            if (row is not null)
            {
                _feed.Unshelve(row.Tag, Operator);
            }
        }

        private void ShowDetail(AlarmRow? row)
        {
            if (row is not null)
            {
                SelectedRow = row;
            }
        }

        #endregion

        #region 详情档案后台预取（#31）

        /// <summary>下单取档（#31）：UI 线程只下单——档案由取档接缝在后台去库里拿，取回编队回 UI；
        /// 换行对暗号（代数自增），慢档归位时选中已换人就作废不出手。置空选中即收弹窗，
        /// 在途的旧单作废；取档炸了停在取档处（故障不炸上 UI），弹窗空着——重按同一行即重新下单。</summary>
        private void FetchDetail(string? tag)
        {
            var gen = ++_detailGen;
            if (tag is null)
            {
                Detail = null;                              // 收起弹窗；在途旧单已被换代作废
                return;
            }

            _runOffUiThread(() =>
            {
                AlarmDefinition? definition = null;
                try
                {
                    definition = _store.Find(tag);
                }
                catch
                {
                    // 库暂时进不去（busy 超时/磁盘抖动）：档没取到就当没取到，故障停在取档处——
                    // 不许炸上 UI 线程；弹窗空着，操作员重按同一行即重新下单
                }

                _ui.Post(_ =>
                {
                    if (gen == _detailGen)                  // 对暗号：中间没换过选中，这份档案才配归位
                    {
                        Detail = definition;
                    }
                }, null);
            });
        }

        #endregion

        #region 事件直译与清单重切

        /// <summary>活跃快照对账：行登记整体重建自 feed.Active（进页/回页对账，事件流断档的兜底）。
        /// 整体重建是行实例终生制的唯一例外，两清单一并清空重填——Refill 的原位对账只认位号，
        /// 留着上一代的行会跟新实例同位号双行（#25 评审），所以这里不走 Refill 的留旧路径。
        /// 走完按位号重锚选中（#25）。</summary>
        public void Reload()
        {
            _rows.Clear();
            MainRows.Clear();
            ShelvedRows.Clear();
            foreach (ActiveAlarm member in _feed.Active)
            {
                _rows[member.Tag] = new AlarmRow(member);
            }

            RefreshFilters();

            // 重锚选中（#25）：行登记重建后旧实例已死，开着的详情弹窗不能绑死旧行——
            // 按位号认领新行（setter 顺带重取档案，详情拿到新库态）；位号不在了就置 null 收弹窗。
            if (SelectedRow is not null)
            {
                SelectedRow = _rows.GetValueOrDefault(SelectedRow.Tag);
            }
        }

        /// <summary>重切两清单：主列表/搁置清单 = AlarmFilters 对同一份行的两个视图（到期标自然失效在呈现侧的重算入口）。
        /// 排序按档案 Priority（高优永远在顶），同优先级内按活跃起点先后；清单原位对账（见 <see cref="Refill"/>）。</summary>
        public void RefreshFilters()
        {
            var now = _now();
            var snapshot = _rows.Values
                .Select(row => row.ToActiveAlarm())
                .OrderBy(a => Rank(a.Priority))
                .ThenBy(a => a.Since)
                .ToList();
            Refill(MainRows, AlarmFilters.MainList(snapshot, now));
            Refill(ShelvedRows, AlarmFilters.Shelved(snapshot, now));
        }

        /// <summary>优先级排序权重（呈现层排序，不定级）：高 0 / 中 1 / 低 2——高优永远在顶。</summary>
        private static int Rank(AlarmPriority priority) => priority switch
        {
            AlarmPriority.High => 0,
            AlarmPriority.Medium => 1,
            _ => 2,
        };

        /// <summary>事件直译（Apply 零判定）：只做集合变更，规矩全部来自引擎产出。</summary>
        private void Apply(AlarmEvent e)
        {
            switch (e)
            {
                case AlarmRaised raised:
                    // 升级（含 RTN 再激活）：行存在则原位刷（状态回未确认、起点/标照事件刷新），
                    // 行实例不换——选中与详情弹窗不脱钩；首升才造行。
                    if (_rows.TryGetValue(raised.Tag, out var existing))
                    {
                        existing.Reraise(raised.At, raised.ShelvedUntil);
                    }
                    else
                    {
                        _rows[raised.Tag] = new AlarmRow(raised.Tag, AlarmState.Unacknowledged, raised.Priority, raised.At, raised.ShelvedUntil);
                    }

                    break;
                case AlarmAcknowledged:
                    UpdateState(e.Tag, AlarmState.Acknowledged);                // 稳红：责任接管
                    break;
                case AlarmAutoReturned:
                    UpdateState(e.Tag, AlarmState.ReturnToNormal);              // 闪绿：留列表等确认
                    break;
                case AlarmClosed:
                    _rows.Remove(e.Tag);                                        // Closed 移出：引擎定的规矩
                    if (SelectedRow?.Tag == e.Tag)
                    {
                        SelectedRow = null;                                     // 详情开着就一并收起：报警已闭环，档案别再挡屏
                    }

                    break;
                case AlarmShelved shelved:
                    UpdateMark(shelved.Tag, shelved.ShelvedUntil);              // 带标：退到搁置清单
                    break;
                case AlarmUnshelved:
                    UpdateMark(e.Tag, null);                                    // 撤标：回主列表
                    break;
            }

            RefreshFilters();
        }

        /// <summary>状态事件的原位修正（行不存在则忽略——引擎不会为缺席的行发状态事件；确认不动搁置标）。</summary>
        private void UpdateState(string tag, AlarmState state)
        {
            if (_rows.TryGetValue(tag, out var row))
            {
                row.SetState(state);
            }
        }

        /// <summary>搁置事件的原位修正（行不存在则忽略——升级事件的标随后会带出来）。</summary>
        private void UpdateMark(string tag, DateTimeOffset? until)
        {
            if (_rows.TryGetValue(tag, out var row))
            {
                row.SetMark(until);
            }
        }

        /// <summary>清单原位对账：移除不在名单里的行、按快照次序插入/搬移——不 Clear 重填
        /// （容器不重建：闪烁动画不重启、ListView 选中不丢）。成员都来自行登记，按位号直取。</summary>
        private void Refill(ObservableCollection<AlarmRow> target, IReadOnlyList<ActiveAlarm> source)
        {
            for (var i = target.Count - 1; i >= 0; i--)
            {
                if (source.All(member => member.Tag != target[i].Tag))
                {
                    target.RemoveAt(i);
                }
            }

            for (var i = 0; i < source.Count; i++)
            {
                var row = _rows[source[i].Tag];
                var current = target.IndexOf(row);
                if (current < 0)
                {
                    target.Insert(i, row);
                }
                else if (current != i)
                {
                    target.Move(current, i);
                }
            }
        }

        #endregion

        /// <summary>收口：作废在途取档（代数换代，慢档归位不出手）再退订事件流——
        /// feed 长命而页面短命时由宿主收口，废弃的 VM 不再被引擎喂养、也不被旧单复活。</summary>
        public void Dispose()
        {
            _detailGen++;
            _feed.EventRaised -= OnEventRaised;
        }
    }
}
