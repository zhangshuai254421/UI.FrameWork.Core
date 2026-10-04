using Framework.Alarm;
using Framework.Alarm.Domain;

namespace SemiAppliaction.Views
{
    /// <summary>
    /// 模拟采集层（#15 演示装置）：<see cref="IConditionSource"/> 的剧本实现——
    /// 只报条件事实，不懂「报警」二字；越没越限由真引擎对照档案判定（判定权只住引擎层）。
    /// 页面 VM 用后台泵线每拍调 <see cref="Report"/> 系成员推进剧本（#29：泵在后台线程，
    /// 事件由列表 VM 编队回 UI）——真宿主的采集层也从采集线程泵，演示页同规矩。
    /// </summary>
    public sealed class SimulatedConditionSource : IConditionSource
    {
        /// <summary>条件事实出口：真引擎订在这里。</summary>
        public event Action<string, ConditionSample>? ConditionChanged;

        /// <summary>报一条条件事实（演示剧本的下一拍）。</summary>
        public void Report(string tag, double value, DateTimeOffset at)
            => ConditionChanged?.Invoke(tag, new ConditionSample(at, value));
    }

    /// <summary>
    /// 报警演示装置（#15 总装，#22 通电）：真引擎 + 三条演示档案 + 循环剧本——
    /// 演示的是产品不是 mock：报警判定、状态机、搁置打标、落库全走 Framework.Alarm 的真代码，
    /// 唯一假装的是「现场」（采集层由剧本代替、时钟用墙钟）；档案与事件史已通电走真库，
    /// 库由宿主建好（AlarmDatabase.Initialize，#22），唯一还替身着的是采集层。
    /// 剧本一循环 9 拍（每拍 1 秒）：升温/憋压/液位上涨先后触发三条报警 → 保持两拍给操作员处置
    /// （确认/搁置在页面上点）→ 第 7 拍（下标 6）全体深回：恢复延时全是零，恢复即 RTN——
    /// 未确认的转闪绿留列表，已确认的当场闭环出列；末两拍停留观察，然后循环再升级。
    /// </summary>
    public sealed class AlarmDemoRig : IDisposable
    {
        /// <summary>演示位号：加热板温度高限（高优，触发延时 2 秒）。</summary>
        public const string HeatTag = "HEAT01_TEMP_HI";

        /// <summary>演示位号：反应釜压力高限（中优，即时触发）。</summary>
        public const string PressTag = "PRESS02_HI";

        /// <summary>演示位号：储罐液位高限（低优，触发延时 1 秒）。</summary>
        public const string LiquidTag = "LIQ01_LEVEL_HI";

        /// <summary>剧本：一循环的工艺量三拍手（温度/压力/液位）——数值对着三条档案的限值排的，
        /// 触发时序错开一拍（升温 2s 延时、憋压即时、液位 1s 延时），主列表不至于一拍齐刷刷三行；
        /// 恢复延时全是零：深回第一拍即 Clear（下标 6），末两拍是停留观察拍。</summary>
        private static readonly (double Heat, double Press, double Liquid)[] Script =
        [
            (200, 0.4, 50), // 基线：全体限内
            (255, 0.4, 50), // 升温越限（延时累计起点）
            (255, 0.9, 50), // 憋压越限（即时触发：压力报警升）
            (255, 0.9, 90), // 温度满 2s 延时（温度报警升）；液位越限（延时累计起点）
            (255, 0.9, 90), // 液位满 1s 延时（液位报警升）
            (255, 0.9, 90), // 保持：三行同挂，等操作员确认/搁置
            (200, 0.4, 50), // 全体深回：恢复延时为零，恢复即 Clear——未确认的转 RTN 闪绿，已确认的当场闭环出列
            (200, 0.4, 50), // 停留观察拍（RTN 的等确认；再升级在下一循环）
            (200, 0.4, 50), // 停留观察拍，然后循环
        ];

        /// <summary>模拟采集层：剧本的事实出口。</summary>
        public SimulatedConditionSource Source { get; } = new();

        /// <summary>档案库：三条演示档案随装置就位（#22 通电走宿主建库的真库，测试传内存替身）。</summary>
        public IAlarmDefinitionStore Store { get; }

        /// <summary>事件史：通电即落库（写后队列后台冲账，#31）；测试断言前经 <see cref="History"/> 冲账。null = 不接本子。</summary>
        public IEventStore? Events { get; }

        /// <summary>事件史接线：随装置存亡（#31 落库改后台，冲账/收口经此——null = 没接本子）。</summary>
        public EventHistory? History { get; }

        /// <summary>真引擎：档案装好、采集层接好、墙钟对好——页面拿它当 IAlarmFeed 用。</summary>
        public AlarmEngine Engine { get; }

        /// <summary>剧本游标：循环推进。</summary>
        private int _step;

        /// <summary>
        /// 通电（#22）：档案缺了才补（评审法定产出不改写——库里已有的档案不动，缺的位号补出生证），
        /// 再总装引擎（无档案不报警的前提）；接了事件库则 EventHistory 一行接线——订阅即入队、
        /// 后台冲账（#31），演示的每一拍都进真本子（搁置单重启回放，#16，因此在演示页一并兑现）。
        /// </summary>
        /// <param name="store">档案库：宿主注入真库存储，测试传内存替身。</param>
        /// <param name="events">事件史：宿主注入真库存储；null = 不接本子（测试的内存直通）。</param>
        public AlarmDemoRig(IAlarmDefinitionStore store, IEventStore? events = null)
        {
            Store = store;
            Events = events;
            foreach (var definition in DemoDefinitions().Where(d => store.Find(d.Tag) is null))
            {
                store.Add(definition); // 缺了才补；撞位号当庭炸响（法定校验 + 唯一约束都在）
            }

            Engine = new AlarmEngine(Source, store, new SystemClock(), events);
            if (events is not null)
            {
                History = new EventHistory(events, Engine); // 订阅即入队，后台冲账（摘要 §8.11、#31）：接线随装置存亡
            }
        }

        /// <summary>剧本下一拍：三条条件事实同一时刻报进引擎（采集层一拍报全部点位）。</summary>
        public void PumpNext()
        {
            var now = DateTimeOffset.Now;
            var (heat, press, liquid) = Script[_step++ % Script.Length];
            Source.Report(HeatTag, heat, now);
            Source.Report(PressTag, press, now);
            Source.Report(LiquidTag, liquid, now);
        }

        /// <summary>收口（#31）：先冲账（本子不欠账，队列尾部不随进程散）再退订采集层——
        /// 接线随装置存亡，收口也是；页面随应用存亡，规范起见随宿主处置。</summary>
        public void Dispose()
        {
            History?.Dispose();
            Engine.Dispose();
        }

        /// <summary>三条演示档案（哲学 §6.1：操作动作必填——填不出的候选当庭退回）。</summary>
        private static IEnumerable<AlarmDefinition> DemoDefinitions()
        {
            yield return new AlarmDefinition(
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
                RationalizedOn: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PhilosophyVersion: "1.0");

            yield return new AlarmDefinition(
                Tag: PressTag,
                Description: "反应釜压力高于安全上限",
                Class: AlarmClass.Safety,
                Consequence: "超压撕裂防爆膜，全釜物料报废",
                Cause: "放料阀误关或反应失控",
                OperatorAction: "开泄压阀，紧急冷却",
                MaxResponseTime: TimeSpan.FromMinutes(2),
                Severity: AlarmSeverity.S1,
                MtrTier: ResponseTimeTier.T1,
                Priority: AlarmPriority.Medium,
                Limit: 0.8,
                Deadband: 0.05,
                OnDelay: TimeSpan.Zero,
                OffDelay: TimeSpan.Zero,
                Group: "反应区",
                RationalizedBy: "李四",
                RationalizedOn: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PhilosophyVersion: "1.0");

            yield return new AlarmDefinition(
                Tag: LiquidTag,
                Description: "储罐液位高于溢流线",
                Class: AlarmClass.Equipment,
                Consequence: "物料溢流，地面污染",
                Cause: "进料阀卡滞或液位计漂移",
                OperatorAction: "关小进料阀，启动转料泵",
                MaxResponseTime: TimeSpan.FromMinutes(10),
                Severity: AlarmSeverity.S3,
                MtrTier: ResponseTimeTier.T2,
                Priority: AlarmPriority.Low,
                Limit: 80,
                Deadband: 3,
                OnDelay: TimeSpan.FromSeconds(1),
                OffDelay: TimeSpan.Zero,
                Group: "罐区",
                RationalizedBy: "王五",
                RationalizedOn: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PhilosophyVersion: "1.0");
        }
    }
}
