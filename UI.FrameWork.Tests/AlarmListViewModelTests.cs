using System.IO;
using Framework.Alarm;
using Framework.Alarm.Domain;

namespace SemiAppliaction.Views.Tests
{
    /// <summary>
    /// AlarmListViewModel 契约测试（摘要 §9.3，哲学 §3.2 呈现纪律【强制】）：Apply 里零判定——
    /// 事件到集合变更的直译（Raised→Add、Acknowledged/AutoReturned→UpdateState、Closed→Remove、
    /// Shelved/Unshelved→UpdateMark）；排序按档案 Priority；主列表/搁置清单 = 引擎库 AlarmFilters 的两个视图。
    /// 测试从接缝进：FakeFeed/同步 SynchronizationContext/注入 now——不依赖真引擎、真线程、真睡等待（#15 验收）。
    /// </summary>
    public class AlarmListViewModelTests
    {
        /// <summary>演示位号：温度上限（引擎侧同款示例）。</summary>
        private const string HeatTag = "HEAT01_TEMP_HI";

        /// <summary>演示位号：泵停运（中优）。</summary>
        private const string PumpTag = "PUMP01_STOP";

        /// <summary>基准时刻：测试用它加秒数拨表。</summary>
        private static DateTimeOffset T(double seconds)
            => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(seconds);

        /// <summary>同步编队替身：Post 即执行——跨线程编队用同步路径绕过（摘要 §9.2 测试技巧）。</summary>
        private sealed class SyncContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object? state) => d(state);
        }

        /// <summary>馈送替身：发事件、记动作留证（确认/搁置/解除的转手证据），Active 快照可设定。</summary>
        private sealed class FakeFeed : IAlarmFeed
        {
            public event Action<AlarmEvent>? EventRaised;

            /// <summary>Active 快照：默认空，由测试设定。</summary>
            public IReadOnlyList<ActiveAlarm> Active { get; set; } = [];

            /// <summary>动作留证：每笔转手动作一行。</summary>
            public List<string> Calls { get; } = [];

            public void Acknowledge(string tag, string op) => Calls.Add($"Ack:{tag}:{op}");

            public void Shelve(string tag, TimeSpan duration, string op) => Calls.Add($"Shelve:{tag}:{duration}:{op}");

            public void Unshelve(string tag, string op) => Calls.Add($"Unshelve:{tag}:{op}");

            public void Dispose()
            {
            }

            /// <summary>模拟引擎吐事件。</summary>
            public void Emit(AlarmEvent e) => EventRaised?.Invoke(e);
        }

        /// <summary>档案替身：只实现 Find（详情弹窗的数据源），载两条演示档案；
        /// Find 留了线程号（#31：档案取用不许占选中线程）与炸点开关（故障不炸屏的考官）。</summary>
        private sealed class FakeStore : IAlarmDefinitionStore
        {
            private readonly Dictionary<string, AlarmDefinition> _items = new();

            public FakeStore()
            {
                _items[HeatTag] = Sample();
                _items[PumpTag] = PumpSample();
            }

            /// <summary>Find 炸点：置真即每次取档都炸（模拟库暂时进不去）。</summary>
            public bool FindThrows { get; set; }

            /// <summary>Find 跑过的线程号（#31：默认接线必须是后台线程）。</summary>
            public List<int> FindThreadIds { get; } = [];

            public void Add(AlarmDefinition definition) => throw new NotSupportedException();

            public AlarmDefinition? Find(string tag)
            {
                FindThreadIds.Add(Environment.CurrentManagedThreadId);
                if (FindThrows)
                {
                    throw new IOException("库暂时进不去（模拟外部锁库）");
                }

                return _items.TryGetValue(tag, out var definition) ? definition : null;
            }

            public void Update(AlarmDefinition definition) => throw new NotSupportedException();

            public bool Delete(string tag) => false;

            public IReadOnlyList<AlarmDefinition> LoadAll() => _items.Values.ToList();
        }

        /// <summary>演示档案：HEAT01（哲学 §6.1 示例：限值 250 / 死区 2 / 触发延时 2s / 高优）。</summary>
        private static AlarmDefinition Sample() => new(
            Tag: HeatTag,
            Description: "加热板温度高于工艺上限",
            Class: AlarmClass.Process,
            Consequence: "本炉产品全部报废",
            Cause: "加热管失控或热电偶漂移",
            OperatorAction: "停炉切手动，通知工艺员",
            MaxResponseTime: TimeSpan.FromMinutes(5),
            Severity: AlarmSeverity.S2,
            MtrTier: ResponseTimeTier.T1,
            Priority: AlarmPriority.High,
            Limit: 250,
            Deadband: 2,
            OnDelay: TimeSpan.FromSeconds(2),
            OffDelay: TimeSpan.Zero,
            Group: "加热区",
            RationalizedBy: "张三",
            RationalizedOn: T(0),
            PhilosophyVersion: "1.0");

        /// <summary>演示档案：PUMP01（中优，换行选中的另一头）。</summary>
        private static AlarmDefinition PumpSample() => new(
            Tag: PumpTag,
            Description: "泵停运联锁",
            Class: AlarmClass.Equipment,
            Consequence: "供料中断",
            Cause: "停机联锁或泵故障",
            OperatorAction: "确认停泵原因",
            MaxResponseTime: TimeSpan.FromMinutes(10),
            Severity: AlarmSeverity.S3,
            MtrTier: ResponseTimeTier.T2,
            Priority: AlarmPriority.Medium,
            Limit: 0,
            Deadband: 0,
            OnDelay: TimeSpan.Zero,
            OffDelay: TimeSpan.Zero,
            Group: "泵区",
            RationalizedBy: "李四",
            RationalizedOn: T(0),
            PhilosophyVersion: "1.0");

        /// <summary>造一台被测：注入替身馈送、档案库、同步编队与固定 now；取档默认内联（断言同步到位），
        /// 要验后台取档的测试经 <paramref name="runOffUiThread"/> 换接缝或直造。</summary>
        private static (AlarmListViewModel Vm, FakeFeed Feed, FakeStore Store) NewVm(
            Func<DateTimeOffset>? now = null,
            Action<Action>? runOffUiThread = null)
        {
            var feed = new FakeFeed();
            var store = new FakeStore();
            var vm = new AlarmListViewModel(
                feed, store, new SyncContext(), now ?? (Func<DateTimeOffset>)(() => T(0)),
                runOffUiThread ?? (a => a()));
            return (vm, feed, store);
        }

        // ══════════════════════ 事件 → 集合直译（Apply 零判定） ══════════════════════

        /// <summary>
        /// 验收「模拟 feed 发事件 → 列表出现正确」：Raised → 主列表出现一行，
        /// State=Unacknowledged（闪红）、优先级与活跃起点照单全收引擎产出。
        /// </summary>
        [Fact]
        public void 升级_主列表出现一行_未确认()
        {
            var (vm, feed, _) = NewVm();

            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));

            var row = Assert.Single(vm.MainRows);
            Assert.Equal(HeatTag, row.Tag);
            Assert.Equal(AlarmState.Unacknowledged, row.State);
            Assert.Equal(AlarmPriority.High, row.Priority);
            Assert.Equal(T(2), row.Since);
            Assert.Empty(vm.ShelvedRows);
        }

        /// <summary>Raised 带标（搁置中触发）：进搁置清单、不进主列表——UI 按标过滤（摘要 §8.10）。</summary>
        [Fact]
        public void 升级_带标_进搁置清单不进主列表()
        {
            var (vm, feed, _) = NewVm();

            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High, ShelvedUntil: T(32)));

            Assert.Empty(vm.MainRows);
            var row = Assert.Single(vm.ShelvedRows);
            Assert.Equal(HeatTag, row.Tag);
            Assert.Equal(T(32), row.ShelvedUntil);
        }

        /// <summary>
        /// 验收「变色正确」之一：Acknowledged → 同一行实例 State 变 Acknowledged（稳红），
        /// 仍留主列表——UI 只换状态，不增删行；「RTN 留列表、Closed 移出」的规矩引擎定，UI 照单全收。
        /// </summary>
        [Fact]
        public void 确认_同一行变稳红()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            var raised = vm.MainRows[0];

            feed.Emit(new AlarmAcknowledged(HeatTag, T(3), "李四"));

            Assert.Single(vm.MainRows);
            Assert.Same(raised, vm.MainRows[0]);                            // 同一行实例：列表不闪、选中不丢
            Assert.Equal(AlarmState.Acknowledged, vm.MainRows[0].State);    // 三态样式绑它
        }

        /// <summary>验收「变色正确」之二：AutoReturned → 同一行 State 变 RTN（闪绿），仍留列表。</summary>
        [Fact]
        public void 自动恢复_同一行变闪绿()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            var raised = vm.MainRows[0];

            feed.Emit(new AlarmAutoReturned(HeatTag, T(2.5)));

            Assert.Same(raised, vm.MainRows[0]);
            Assert.Equal(AlarmState.ReturnToNormal, vm.MainRows[0].State);
        }

        /// <summary>RTN 再激活：未确认自愈的行还挂着（RTN 闪绿），条件再越限触发再激活——
        /// 同一行实例原位刷回未确认、活跃起点照事件刷新，行不换实例（选中与详情弹窗不脱钩）。</summary>
        [Fact]
        public void 再激活_同一行回未确认_起点刷新()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            feed.Emit(new AlarmAutoReturned(HeatTag, T(4)));               // 未确认自愈：闪绿留列表
            var row = vm.MainRows[0];

            feed.Emit(new AlarmRaised(HeatTag, T(6), AlarmPriority.High)); // 条件再越限：再激活

            Assert.Single(vm.MainRows);
            Assert.Same(row, vm.MainRows[0]);                              // 行实例不换：原位刷
            Assert.Equal(AlarmState.Unacknowledged, row.State);            // 回闪红
            Assert.Equal(T(6), row.Since);                                 // 起点刷新
        }

        /// <summary>验收「移除正确」：Closed → 行从两清单里消失（Closed 移出是引擎定的规矩，UI 照单全收）。</summary>
        [Fact]
        public void 闭环_移出列表()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            feed.Emit(new AlarmAcknowledged(HeatTag, T(3), "李四"));

            feed.Emit(new AlarmClosed(HeatTag, T(4)));

            Assert.Empty(vm.MainRows);
            Assert.Empty(vm.ShelvedRows);
        }

        /// <summary>闭环时开着的详情弹窗一并收起：报警已闭环，档案浮层别再挡屏（也不留一行已消失的报警的档案）。</summary>
        [Fact]
        public void 闭环_开着的详情一并收起()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            vm.ShowDetailCommand.Execute(vm.MainRows[0]);
            Assert.NotNull(vm.Detail);

            feed.Emit(new AlarmClosed(HeatTag, T(4)));

            Assert.Null(vm.SelectedRow);
            Assert.Null(vm.Detail);
        }

        /// <summary>操作员闭环事件同规矩移出列表（#26）：AlarmClosedByOperator 派生自 AlarmClosed，
        /// 呈现层的类型匹配照旧吃它——闭环语义不因分型而变，VM 不认得新事件照样直译。</summary>
        [Fact]
        public void 操作员闭环_同规矩移出列表()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            feed.Emit(new AlarmAutoReturned(HeatTag, T(2.5)));

            feed.Emit(new AlarmClosedByOperator(HeatTag, T(10), "王五"));

            Assert.Empty(vm.MainRows);
            Assert.Empty(vm.ShelvedRows);
        }

        /// <summary>Shelved/Unshelved 事件 → 行标的直译：搁置退到搁置清单，解除回主列表（事件驱动，无轮询）。</summary>
        [Fact]
        public void 搁置与解除事件_行在两清单间移动()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));

            feed.Emit(new AlarmShelved(HeatTag, T(3), T(33), "李四"));
            Assert.Empty(vm.MainRows);                                      // 主列表按标滤出
            Assert.Single(vm.ShelvedRows);

            feed.Emit(new AlarmUnshelved(HeatTag, T(4), "王五"));
            Assert.Single(vm.MainRows);                                     // 解除：回主列表正常播报
            Assert.Empty(vm.ShelvedRows);
        }

        /// <summary>
        /// 到期不用解除：拨表后 RefreshFilters 重切两清单——标过期自然回主列表（摘要 §8.10
        /// 「时钟一过标自然失效」在呈现侧的兑现，规则来自引擎库 AlarmFilters，VM 无自定过滤）。
        /// </summary>
        [Fact]
        public void 到期_拨表刷新_自动回主列表()
        {
            DateTimeOffset now = T(0);
            var (vm, feed, _) = NewVm(now: () => now);
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            feed.Emit(new AlarmShelved(HeatTag, T(3), ShelvedUntil: T(30), "李四"));
            Assert.Single(vm.ShelvedRows);

            now = T(31);                                                    // 拨表过期限
            vm.RefreshFilters();

            Assert.Single(vm.MainRows);
            Assert.Empty(vm.ShelvedRows);                                   // 无 Unshelved 事件也回主列表
        }

        /// <summary>
        /// 排序按档案 Priority（哲学 §3.2）：低优先升、高优后升——高优永远在顶，
        /// 同优先级内按活跃起点先后（次序规则来自档案字段，UI 不定级）。
        /// </summary>
        [Fact]
        public void 排序_按优先级_高优在顶()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(PumpTag, T(2), AlarmPriority.Medium));
            feed.Emit(new AlarmRaised(HeatTag, T(3), AlarmPriority.High));
            feed.Emit(new AlarmRaised("LIQ01_LEVEL_HI", T(4), AlarmPriority.Low));

            Assert.Equal(
                new[] { HeatTag, PumpTag, "LIQ01_LEVEL_HI" },
                vm.MainRows.Select(r => r.Tag));
        }

        /// <summary>还原：从 feed.Active 快照重建两清单（进页对账）——带标成员进搁置清单，其余进主列表。</summary>
        [Fact]
        public void 还原_从活跃快照重建两清单()
        {
            var (vm, feed, _) = NewVm();
            feed.Active =
            [
                new ActiveAlarm(PumpTag, AlarmState.Acknowledged, AlarmPriority.Medium, T(2)),
                new ActiveAlarm(HeatTag, AlarmState.Unacknowledged, AlarmPriority.High, T(5), ShelvedUntil: T(35)),
            ];

            vm.Reload();

            var main = Assert.Single(vm.MainRows);
            Assert.Equal(PumpTag, main.Tag);
            Assert.Equal(AlarmState.Acknowledged, main.State);
            var shelved = Assert.Single(vm.ShelvedRows);
            Assert.Equal(HeatTag, shelved.Tag);
            Assert.Equal(T(35), shelved.ShelvedUntil);
        }

        /// <summary>回页对账重锚选中（#25）：Reload 重建行登记后，开着的详情弹窗认领新实例——
        /// 不绑死被遗弃的旧行（引擎后续事件刷的是新实例，弹窗绑旧行就冻死在旧状态）。</summary>
        [Fact]
        public void 还原_重锚选中行_详情跟随新实例()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            vm.ShowDetailCommand.Execute(vm.MainRows[0]);
            var stale = vm.SelectedRow;

            feed.Active = [new ActiveAlarm(HeatTag, AlarmState.Unacknowledged, AlarmPriority.High, T(2))];
            vm.Reload();

            Assert.NotNull(vm.SelectedRow);
            Assert.NotSame(stale, vm.SelectedRow);                          // 旧实例已弃：选中认领新行
            Assert.Same(vm.MainRows[0], vm.SelectedRow);                    // 指向重建后的行
            Assert.NotNull(vm.Detail);                                      // 弹窗开着：档案重取不脱钩
        }

        /// <summary>回页对账时位号已不在活跃快照：开着的详情收起——不留悬空选中与挡屏弹窗（#25）。</summary>
        [Fact]
        public void 还原_选中位号已消失_收起弹窗()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            vm.ShowDetailCommand.Execute(vm.MainRows[0]);

            feed.Active = [];                                               // 对账快照里没了（已闭环/被摘）
            vm.Reload();

            Assert.Null(vm.SelectedRow);
            Assert.Null(vm.Detail);
        }

        /// <summary>回页对账不复活旧行（#25 评审）：集合里已有事件喂出的同位号行时，Reload 整体重建
        /// 必须连旧实例一并清走——Refill 按位号对账只删「快照里没有的」，旧实例不清理就同位号双行，
        /// 旧行还被 Apply 遗弃冻死在旧状态。</summary>
        [Fact]
        public void 还原_活页再对账_同位号不双行()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));  // 事件已喂出一行（活页）
            var stale = vm.MainRows[0];

            feed.Active = [new ActiveAlarm(HeatTag, AlarmState.Unacknowledged, AlarmPriority.High, T(2))];
            vm.Reload();

            var row = Assert.Single(vm.MainRows);                           // 旧行不能阴魂不散
            Assert.NotSame(stale, row);                                     // 留下的必须是重建的新实例
        }

        // ══════════════════════ 操作员动作转手 feed（UI 不自改状态） ══════════════════════

        /// <summary>确认命令：转手 feed.Acknowledge(tag, op)——VM 不自己改状态，等引擎的 Acknowledged 事件回弹。</summary>
        [Fact]
        public void 确认命令_转手feed_不自改状态()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            var row = vm.MainRows[0];

            vm.AcknowledgeCommand.Execute(row);

            Assert.Equal([$"Ack:{HeatTag}:演示员"], feed.Calls);             // 署名随动作转手
            Assert.Equal(AlarmState.Unacknowledged, vm.MainRows[0].State); // VM 没自改：等引擎事件回弹
        }

        /// <summary>搁置命令：转手 feed.Shelve(tag, ShelveDuration, op)——时长由 UI 供给且必须限时。</summary>
        [Fact]
        public void 搁置命令_带时长与署名转手()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            var row = vm.MainRows[0];
            vm.ShelveDuration = TimeSpan.FromMinutes(5);

            vm.ShelveCommand.Execute(row);

            Assert.Equal([$"Shelve:{HeatTag}:00:05:00:演示员"], feed.Calls);
        }

        /// <summary>解除命令：转手 feed 接口上的 Unshelve(tag, op)——搁置清单行上的「解除搁置」按钮。</summary>
        [Fact]
        public void 解除命令_转手feed()
        {
            var (vm, feed, _) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            feed.Emit(new AlarmShelved(HeatTag, T(3), T(33), "李四"));      // 先搁上：行进搁置清单

            vm.UnshelveCommand.Execute(vm.ShelvedRows[0]);

            Assert.Equal([$"Unshelve:{HeatTag}:演示员"], feed.Calls);        // 只转手动作，等引擎事件回弹
        }

        /// <summary>
        /// 详情弹窗 = MADB 档案原样呈现（哲学 §3.2）：选中行的档案整行给出——
        /// 原因/后果/操作动作/MTR/优先级坐标/评审留痕一个不缺，第三问在这一屏兑现。
        /// </summary>
        [Fact]
        public void 详情_档案整行呈现()
        {
            var (vm, feed, store) = NewVm();
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));

            vm.ShowDetailCommand.Execute(vm.MainRows[0]);

            var detail = vm.Detail;
            Assert.NotNull(detail);
            Assert.Equal(HeatTag, detail.Tag);
            Assert.Equal("加热板温度高于工艺上限", detail.Description);       // 发生了什么
            Assert.Equal("加热管失控或热电偶漂移", detail.Cause);             // 最可能原因
            Assert.Equal("本炉产品全部报废", detail.Consequence);             // 不处理后果
            Assert.Equal("停炉切手动，通知工艺员", detail.OperatorAction);    // 第三问的答案：操作动作
            Assert.Equal(TimeSpan.FromMinutes(5), detail.MaxResponseTime);   // MTR
            Assert.Equal(ResponseTimeTier.T1, detail.MtrTier);               // 优先级坐标（S2 × T1）
            Assert.Equal("张三", detail.RationalizedBy);                      // 评审留痕
        }

        /// <summary>慢档不出手（#31 读侧）：取档在后台走，选中已换人时回来的旧档不得闪进弹窗——
        /// 代数对暗号（同泵线规矩），新档归位旧档作废。</summary>
        [Fact]
        public void 详情_慢档未归_换行选中_旧档不出手()
        {
            var pending = new List<Action>();                            // 手动泵：取档下单压着不放
            var feed = new FakeFeed();
            var store = new FakeStore();
            var vm = new AlarmListViewModel(feed, store, new SyncContext(), runOffUiThread: a => pending.Add(a));
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));
            feed.Emit(new AlarmRaised(PumpTag, T(3), AlarmPriority.Medium));

            vm.ShowDetailCommand.Execute(vm.MainRows[0]);                // 选中热板：取档下单（档未归）
            vm.ShowDetailCommand.Execute(vm.MainRows[1]);                // 换选泵：又一单
            Assert.Null(vm.Detail);                                      // 档未归：弹窗空着，不许拿空当有档案

            pending[0]();                                                // 旧档回来：代数已换代
            Assert.Null(vm.Detail);                                      // 不出手——库慢也不闪旧账

            pending[1]();                                                // 新档回来：对上暗号
            Assert.NotNull(vm.Detail);
            Assert.Equal(PumpTag, vm.Detail?.Tag);                       // 弹窗认的是当下选中那位的档案
        }

        /// <summary>默认接线真走后台（#31）：不注取档接缝时（产品接线），Find 不得跑在选中线程上——
        /// 库被外部锁住时 busy 等待冻的是后台线程，不是点了行的 UI。</summary>
        [Fact]
        public void 详情_默认后台取档_不占选中线程()
        {
            var feed = new FakeFeed();
            var store = new FakeStore();
            var vm = new AlarmListViewModel(feed, store, new SyncContext()); // 不注接缝：走产品默认
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));

            var selectedOn = Environment.CurrentManagedThreadId;
            vm.ShowDetailCommand.Execute(vm.MainRows[0]);

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);    // 等后台档归（只影响速度不影响结论）
            while (vm.Detail is null && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(5);
            }

            Assert.NotNull(vm.Detail);
            Assert.NotEqual(selectedOn, Assert.Single(store.FindThreadIds)); // Find 不在选中线程：解耦真发生
        }

        /// <summary>取档炸了不炸屏（#31 读侧）：库暂时进不去（外部锁库的替身）时——故障停在取档处，
        /// 不许炸上 UI 线程；弹窗保持收起，等下一次点行重新下单。</summary>
        [Fact]
        public void 详情_取档炸了_不炸屏()
        {
            var (vm, feed, store) = NewVm();
            store.FindThrows = true;
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));

            vm.ShowDetailCommand.Execute(vm.MainRows[0]);                // 内联接线：炸点当场现形

            Assert.Null(vm.Detail);                                      // 档没取到：弹窗保持收起
        }

        /// <summary>取档没归，重按同一行即重新下单（#31 评审）：上次取档炸了弹窗空着，库缓过来后
        /// 操作员再点同一行——选中引用没变也得重新下单，不许把「换个行再换回来」当唯一出路。</summary>
        [Fact]
        public void 详情_取档炸了_重按同一行_重新下单()
        {
            var pending = new List<Action>();                            // 手动泵：下单压着不放
            var feed = new FakeFeed();
            var store = new FakeStore();
            var vm = new AlarmListViewModel(feed, store, new SyncContext(), runOffUiThread: a => pending.Add(a));
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));

            store.FindThrows = true;                                     // 头一单赶上库忙
            vm.ShowDetailCommand.Execute(vm.MainRows[0]);
            pending[0]();
            Assert.Null(vm.Detail);                                      // 没取到：弹窗空着

            store.FindThrows = false;                                    // 库缓过来了
            vm.ShowDetailCommand.Execute(vm.MainRows[0]);                // 重按同一行（引用没变）
            pending[1]();                                                // 第二单真下了、也真归位

            Assert.NotNull(vm.Detail);                                   // 重新下单兑现
            Assert.Equal(HeatTag, vm.Detail?.Tag);
        }

        /// <summary>收口作废在途取档（#31 评审）：VM 处置后慢档才归位——不许给废弃的 VM 喂档案。</summary>
        [Fact]
        public void 收口_在途取档作废()
        {
            var pending = new List<Action>();
            var feed = new FakeFeed();
            var store = new FakeStore();
            var vm = new AlarmListViewModel(feed, store, new SyncContext(), runOffUiThread: a => pending.Add(a));
            feed.Emit(new AlarmRaised(HeatTag, T(2), AlarmPriority.High));

            vm.ShowDetailCommand.Execute(vm.MainRows[0]);                // 下单（档未归）
            vm.Dispose();                                                // 收口：代数换代
            pending[0]();                                                // 迟到的档案

            Assert.Null(vm.Detail);                                      // 废弃的 VM 不接档
        }
    }
}
