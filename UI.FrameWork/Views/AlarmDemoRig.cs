using Framework.Alarm;
using Framework.Alarm.Domain;

namespace SemiAppliaction.Views
{
    /// <summary>
    /// 模拟采集层（#15 演示装置）：<see cref="IConditionSource"/> 的剧本实现——
    /// 只报条件事实，不懂「报警」二字；有没有越限由真引擎对照档案判定（判定逻辑只在引擎层）。
    /// 页面 VM 用后台循环每秒调一次 <see cref="Report"/> 推进剧本（#29：循环跑在后台线程，
    /// 事件由列表 VM 编队回 UI）——真宿主的采集层也从采集线程报数，演示页同一规矩。
    /// </summary>
    public sealed class SimulatedConditionSource : IConditionSource
    {
        /// <summary>条件事实出口：真引擎订在这里。</summary>
        public event Action<string, ConditionSample>? ConditionChanged;

        /// <summary>报一条条件事实（演示剧本的下一步）。</summary>
        public void Report(string tag, double value, DateTimeOffset at)
            => ConditionChanged?.Invoke(tag, new ConditionSample(at, value));
    }

    /// <summary>
    /// 报警演示装置（#15 总装，#22 接入真库）：真引擎 + 三条演示档案 + 循环剧本——
    /// 演示的是产品不是 mock：报警判定、状态机、搁置、落库全走 Framework.Alarm 的真代码，
    /// 唯一假装的是「现场」（采集层由剧本代替、时钟用墙钟）。档案与事件史已接入真库，
    /// 库由宿主建好（AlarmDatabase.Initialize，#22），唯一还用替身的是采集层。
    /// 剧本一个循环 9 步（每步 1 秒）：升温/憋压/液位上涨先后触发三条报警 → 保持两步给操作员处置
    /// （确认/搁置在页面上点）→ 第 7 步（下标 6）全体恢复：恢复延时全是零，一恢复就 Clear——
    /// 未确认的转闪绿留在列表，已确认的当场闭环移出列表；最后两步停留观察，然后循环再升级。
    /// </summary>
    public sealed class AlarmDemoRig : IDisposable
    {
        /// <summary>演示位号：加热板温度高限（高优先级，触发延时 2 秒）。</summary>
        public const string HeatTag = "HEAT01_TEMP_HI";

        /// <summary>演示位号：反应釜压力高限（中优先级，即时触发）。</summary>
        public const string PressTag = "PRESS02_HI";

        /// <summary>演示位号：储罐液位高限（低优先级，触发延时 1 秒）。</summary>
        public const string LiquidTag = "LIQ01_LEVEL_HI";

        /// <summary>剧本：一个循环内三个点位（温度/压力/液位）的工艺量——数值对着三条档案的限值排的，
        /// 触发时序错开一步（升温 2s 延时、憋压即时、液位 1s 延时），主列表不至于一步齐刷刷出三行；
        /// 恢复延时全是零：恢复的第一步就 Clear（下标 6），最后两步是停留观察。</summary>
        private static readonly (double Heat, double Press, double Liquid)[] Script =
        [
            (200, 0.4, 50), // 基线：全体限内
            (255, 0.4, 50), // 升温越限（延时累计起点）
            (255, 0.9, 50), // 憋压越限（即时触发：压力报警升）
            (255, 0.9, 90), // 温度满 2s 延时（温度报警升）；液位越限（延时累计起点）
            (255, 0.9, 90), // 液位满 1s 延时（液位报警升）
            (255, 0.9, 90), // 保持：三行同挂，等操作员确认/搁置
            (200, 0.4, 50), // 全体恢复：恢复延时为零，一恢复就 Clear——未确认的转 RTN 闪绿，已确认的当场闭环移出列表
            (200, 0.4, 50), // 停留观察（RTN 的等确认；再升级在下一循环）
            (200, 0.4, 50), // 停留观察，然后循环
        ];

        /// <summary>模拟采集层：剧本的事实出口。外部注入时用容器单例（引擎订的同一份，剧本推它才有效），默认自建。</summary>
        public SimulatedConditionSource Source { get; }

        /// <summary>档案库：三条演示档案随装置就位（#22 起走宿主建的真库，测试传内存替身）。</summary>
        public IAlarmDefinitionStore Store { get; }

        /// <summary>事件库：接入即落库（写后队列后台写，#31）；测试断言前经 <see cref="History"/> 写完存量。null = 不接事件库。</summary>
        public IEventStore? Events { get; }

        /// <summary>事件库接线：随装置存亡（#31 落库改后台，写完存量/关闭经此——null = 没接事件库）。</summary>
        public EventHistory? History { get; }

        /// <summary>真引擎：档案装好、采集层接好、时钟接好。默认自建；外部注入时用容器引擎（顶栏桥听的同一个，演示才能点亮顶栏）。</summary>
        public AlarmEngine Engine { get; }

        /// <summary>剧本游标：循环推进。</summary>
        private int _step;

        /// <summary>引擎归属：自己造的随装置处置；容器注入的是应用级单例，装置无权 Dispose。</summary>
        private readonly bool _ownsEngine;

        /// <summary>
        /// 初始化（#22）：演示档案缺了才补（评审法定产出不改写——库里已有的档案不动，缺的位号补建档），
        /// 再组装引擎（「无档案不报警」的前提）。接了事件库则一行接上 EventHistory——订阅即入队、
        /// 后台写库（#31），演示的每条事件都进真库（搁置单重启回放，#16，因此在演示页一并兑现）。
        /// </summary>
        /// <param name="store">档案库：宿主注入真库存储，测试传内存替身。</param>
        /// <param name="events">事件库：宿主注入真库存储；null = 不接事件库（测试的内存直通）。</param>
        /// <param name="engine">外部引擎：传容器里的单例（演示页用容器引擎，顶栏桥与列表听同一份）；
        /// null = 自建（测试直通用）。</param>
        /// <param name="source">模拟采集层：传容器单例（引擎构造时订的就是它），null = 自建。</param>
        public AlarmDemoRig(IAlarmDefinitionStore store, IEventStore? events = null, AlarmEngine? engine = null, SimulatedConditionSource? source = null)
        {
            Source = source ?? new SimulatedConditionSource();
            Store = store;
            Events = events;
            foreach (var definition in DemoDefinitions().Where(d => store.Find(d.Tag) is null))
            {
                store.Add(definition); // 缺了才补；位号冲突会立即抛异常（法定校验 + 唯一约束都在）
            }

            _ownsEngine = engine is null;
            Engine = engine ?? new AlarmEngine(Source, store, new SystemClock(), events);
            if (events is not null)
            {
                History = new EventHistory(events, Engine); // 订阅即入队，后台写库（摘要 §8.11、#31）：接线随装置存亡
            }
        }

        /// <summary>剧本下一步：三条条件事实同一时刻报进引擎（采集层一步报全部点位）。</summary>
        public void PumpNext()
        {
            var now = DateTimeOffset.Now;
            var (heat, press, liquid) = Script[_step++ % Script.Length];
            Source.Report(HeatTag, heat, now);
            Source.Report(PressTag, press, now);
            Source.Report(LiquidTag, liquid, now);
        }

        /// <summary>关闭（#31）：先写完队列存量（事件不随进程丢）再退订采集层——
        /// 接线随装置存亡，关闭也是；页面随应用存亡，规范起见随宿主一起处置。
        /// 容器注入的引擎是应用级单例，装置只退自己的 History，不动引擎。</summary>
        public void Dispose()
        {
            History?.Dispose();
            if (_ownsEngine)
            {
                Engine.Dispose();
            }
        }

        /// <summary>三条演示档案（哲学 §6.1：操作动作必填——填不出的候选直接拒绝入库）。</summary>
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
