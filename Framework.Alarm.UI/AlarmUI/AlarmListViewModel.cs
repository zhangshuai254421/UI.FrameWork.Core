using System.Collections.ObjectModel;
using Framework.Alarm;
using Framework.Alarm.Domain;
using Prism.Commands;
using Prism.Mvvm;

namespace Framework.Alarm.UI
{
    /// <summary>
    /// 报警列表 ViewModel（摘要 §9.3，哲学 §3.2 呈现纪律【强制】）：把引擎事件直接翻译成集合变更，
    /// Apply 里零判定——Raised → Upsert、Acknowledged/AutoReturned → UpdateState、Closed → Remove、
    /// Shelved/Unshelved → UpdateShelvedUntil。「RTN 留在列表、Closed 移出、按期限过滤」这些规矩都是引擎定的，
    /// UI 照单全收；排序按档案 Priority（高优先级永远在顶），过滤调用引擎库的 <see cref="AlarmFilters"/>。
    /// 跨线程投递（摘要 §9.2 选型定案：SynchronizationContext.Post）：引擎在通讯线程发事件，
    /// 本类把每条事件 Post 到注入的上下文再翻译；测试注入同步上下文即同步执行（不依赖真线程）。
    /// 只认 <see cref="IAlarmFeed"/> 接口——测试造一个假 feed 就能跑，不依赖真引擎。
    /// 行实例终生制：事件只原位刷行、清单原位核对（容器不重建，闪烁动画不重启、选中不丢）；
    /// 唯一例外是 <see cref="Reload"/> 整体重建——完成后按位号重新锚定选中（#25），弹窗不绑死旧实例。
    /// 详情档案走后台预取（#31）：选行只是发起，档案由取档接口在后台去库里拿，取回后投递回 UI——
    /// 库被外部锁住时（#29 场景），busy 等待冻住的是后台线程，不是点了行的 UI。换行核对代数
    /// （与演示循环同一规矩），慢档案取回时如果选中已经换了人就作废、不更新。取档案抛异常也停在取档处，不上抛到 UI。
    /// <see cref="Dispose"/> 退订事件流——feed 长命（单例引擎）而页面短命时由宿主调用。
    /// </summary>
    public sealed class AlarmListViewModel : BindableBase, IDisposable
    {
        /// <summary>馈送接口：事件流与操作员动作的唯一通道（UI 不认识引擎的具体类）。</summary>
        private readonly IAlarmFeed _feed;

        /// <summary>档案接口：详情弹窗的 MADB 数据源（档案原样呈现，UI 不加工）。</summary>
        private readonly IAlarmDefinitionStore _store;

        /// <summary>UI 上下文：事件投递到 UI 线程（摘要 §9.2）。</summary>
        private readonly SynchronizationContext _ui;

        /// <summary>读取时刻：喂给 AlarmFilters 比较（测试注入固定值控制时间）。</summary>
        private readonly Func<DateTimeOffset> _now;

        /// <summary>取档接口（#31）：取档在后台跑——产品默认 Task.Run（线程池），测试注入内联委托即同步执行。</summary>
        private readonly Action<Action> _runOffUiThread;

        /// <summary>取档代数（#31）：每次发起自增——慢档案取回时核对代数，选中已换人就作废、不更新。</summary>
        private int _detailGen;

        /// <summary>行登记表：位号 → 行（事件只更新行，两个清单由 Refill 重切）。</summary>
        private readonly Dictionary<string, AlarmRow> _rows = new();

        /// <summary>构造：订阅馈送事件流（Post 投递到 UI 线程），初始重切一次清单。</summary>
        /// <param name="feed">馈送接口：事件流与操作员动作的通道。</param>
        /// <param name="store">档案接口：详情弹窗的数据源（只在取档接口里于后台访问）。</param>
        /// <param name="ui">UI 上下文：事件与取回的档案都投递回这里。</param>
        /// <param name="now">读取时刻：测试控制时间用。</param>
        /// <param name="runOffUiThread">取档接口（#31）：默认 Task.Run 后台线程；测试注入内联委托即同步执行。</param>
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

        /// <summary>订阅随页面生命周期：本 VM 随页面存亡；引擎 Dispose 后事件流自然停止。</summary>
        private void OnEventRaised(AlarmEvent e) => _ui.Post(_ => Apply(e), null);

        #region 绑定集合与选中

        /// <summary>主列表：没有搁置期限、或期限已过的成员——搁置中的报警被滤出，其余正常显示。</summary>
        public ObservableCollection<AlarmRow> MainRows { get; } = [];

        /// <summary>搁置清单：有搁置期限且还没到期的成员——「搁置清单单独可见」由这一列兑现（哲学 §7）。</summary>
        public ObservableCollection<AlarmRow> ShelvedRows { get; } = [];

        /// <summary>选中行的后端字段。</summary>
        private AlarmRow? _selectedRow;

        /// <summary>选中行：详情弹窗的数据锚点。设置即发起取档案（后台，#31），置空即收起弹窗；
        /// 重按同一行也发起——上次取档没回来（库忙/抛异常）时，再点一次就是重新发起。</summary>
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

        /// <summary>操作员署名：动作转手 feed 时随行（宿主接登录态时覆盖）。</summary>
        public string Operator { get; set; } = "操作员";

        /// <summary>搁置时长：UI 供给、必须限时（无限期搁置 = 变相删报警，哲学 §7）。</summary>
        public TimeSpan ShelveDuration { get; set; } = TimeSpan.FromSeconds(30);

        #endregion

        #region 命令（操作员动作只转手 feed，不自改状态）

        /// <summary>确认命令：转手确认，状态等引擎的 Acknowledged 事件回来再更新。</summary>
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

        /// <summary>发起取档（#31）：UI 线程只发起——档案由取档接口在后台去库里拿，取回后投递回 UI；
        /// 换行时代数自增，慢档案归位时核对代数，选中已换人就作废、不更新。置空选中即收弹窗，
        /// 在途的旧单随换代作废；取档抛异常停在取档处（故障不上抛 UI），弹窗空着——重按同一行即重新发起。</summary>
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
                    // 库暂时进不去（busy 超时、磁盘抖动）：档没取到就当没取到，故障停在取档处——
                    // 不许上抛到 UI 线程；弹窗空着，操作员重按同一行即重新发起
                }

                _ui.Post(_ =>
                {
                    if (gen == _detailGen)                  // 核对代数：中间没换过选中，这份档案才配归位
                    {
                        Detail = definition;
                    }
                }, null);
            });
        }

        #endregion

        #region 事件直译与清单重切

        /// <summary>活跃快照核对：行登记表整体重建自 feed.Active（进页/回页时核对，事件流断档的兜底）。
        /// 整体重建是行实例终生制的唯一例外，两个清单一并清空重填——Refill 的原位核对只认位号，
        /// 留着上一代的行会和新建的同位号行形成双行（#25 评审），所以这里不走 Refill 的留旧路径。
        /// 完成后按位号重新锚定选中（#25）。</summary>
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

            // 重锚选中（#25）：行登记表重建后旧实例已不在，开着的详情弹窗不能绑死旧行——
            // 按位号认领新行（setter 顺带重新取档案，详情拿到新的库内状态）；位号不在了就置 null 收弹窗。
            if (SelectedRow is not null)
            {
                SelectedRow = _rows.GetValueOrDefault(SelectedRow.Tag);
            }
        }

        /// <summary>重切两个清单：主列表/搁置清单 = AlarmFilters 对同一批行的两个视图
        /// （搁置到期在显示侧自然失效，这里是重算入口）。排序按档案 Priority（高优先级永远在顶），
        /// 同优先级内按活跃起点先后；清单原位核对（见 <see cref="Refill"/>）。</summary>
        public void RefreshFilters()
        {
            var now = _now();
            var snapshot = _rows.Values
                .Select(row => row.ToActiveAlarm())
                .OrderBy(a => AlarmFilters.Rank(a.Priority))
                .ThenBy(a => a.Since)
                .ToList();
            Refill(MainRows, AlarmFilters.MainList(snapshot, now));
            Refill(ShelvedRows, AlarmFilters.Shelved(snapshot, now));
        }

        /// <summary>事件直译（Apply 零判定）：只做集合变更，规矩全部来自引擎产出。</summary>
        private void Apply(AlarmEvent e)
        {
            switch (e)
            {
                case AlarmRaised raised:
                    // 升级（含 RTN 再激活）：行存在则原位刷新（状态回未确认、起点和期限照事件刷新），
                    // 行实例不换——选中与详情弹窗不脱钩；首次升级才造新行。
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
                    UpdateState(e.Tag, AlarmState.ReturnToNormal);              // 闪绿：留在列表等确认
                    break;
                case AlarmClosed:
                    _rows.Remove(e.Tag);                                        // Closed 移出：引擎定的规矩
                    if (SelectedRow?.Tag == e.Tag)
                    {
                        SelectedRow = null;                                     // 详情开着就一并收起：报警已闭环，档案别再挡屏
                    }

                    break;
                case AlarmShelved shelved:
                    UpdateShelvedUntil(shelved.Tag, shelved.ShelvedUntil);              // 带期限：退到搁置清单
                    break;
                case AlarmUnshelved:
                    UpdateShelvedUntil(e.Tag, null);                                    // 取消期限：回主列表
                    break;
            }

            RefreshFilters();
        }

        /// <summary>状态事件的原位修正（行不存在则忽略——引擎不会为缺席的行发状态事件；确认不影响搁置期限）。</summary>
        private void UpdateState(string tag, AlarmState state)
        {
            if (_rows.TryGetValue(tag, out var row))
            {
                row.SetState(state);
            }
        }

        /// <summary>搁置事件的原位修正（行不存在则忽略——升级事件随后会带上期限）。</summary>
        private void UpdateShelvedUntil(string tag, DateTimeOffset? until)
        {
            if (_rows.TryGetValue(tag, out var row))
            {
                row.SetShelvedUntil(until);
            }
        }

        /// <summary>清单原位核对：移除不在名单里的行、按快照次序插入或搬移——不 Clear 重填
        /// （容器不重建：闪烁动画不重启、ListView 选中不丢）。成员都来自行登记表，按位号直取。</summary>
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

        /// <summary>收尾：作废在途取档（代数换代，慢档案归位不再更新）再退订事件流——
        /// feed 长命而页面短命时由宿主调用，废弃的 VM 不再被引擎喂数据、也不被在途旧取档更新。</summary>
        public void Dispose()
        {
            _detailGen++;
            _feed.EventRaised -= OnEventRaised;
        }
    }
}
