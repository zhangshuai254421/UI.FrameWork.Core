using Framework.Alarm;
using Framework.Alarm.Domain;

namespace SemiAppliaction.Views.Tests
{
    /// <summary>
    /// 演示装置直通联测：真引擎（AlarmDemoRig）→ 真 AlarmListViewModel，中间不经任何替身——
    /// 单层契约测试各绿不等于接线通（页面「两清单一直空」的排查钉子）：引擎升没升（Active）、
    /// 呈现接没接住（MainRows）一层一个断言，断点在哪一层当场现形。
    /// #22 通电后档案/事件史走注入的存储：测试传内存替身（不碰宿主真库），
    /// 真库链路由 Framework.Alarm.Tests 的契约测试与建库口联测钉住。
    /// </summary>
    public class AlarmDemoRigTests
    {
        /// <summary>事件史替身：只记录，供「通电即落库」断言——录音笔不是契约替身（契约双生归
        /// Framework.Alarm.Tests 的 InMemoryEventStore，本工程够不着也无需同一份）。</summary>
        private sealed class RecordingEventStore : IEventStore
        {
            public List<AlarmEvent> Events { get; } = [];

            public void Append(AlarmEvent e) => Events.Add(e);

            public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
                => Events.Where(e => e.At >= from && e.At <= to
                                  && (tag is null || e.Tag == tag)
                                  && (kinds.Length == 0 || kinds.Contains(e.GetType()))).ToList();
        }

        /// <summary>
        /// 真装置直通：剧本推五拍（憋压即时触发档）——引擎 Active 里得有报警，
        /// 主列表里也得有同一行。两清单一直空的病根要么在引擎层（Active 空），要么在直译层（MainRows 空）。
        /// </summary>
        [Fact]
        public void 真装置直通_剧本五拍_引擎升了主列表接住()
        {
            var rig = new AlarmDemoRig(new InMemoryAlarmDefinitionStore());
            var vm = new AlarmListViewModel(rig.Engine, rig.Store, new SyncContext());

            for (var beat = 0; beat < 5; beat++)
            {
                rig.PumpNext();
                vm.RefreshFilters();
            }

            var active = Assert.Single(rig.Engine.Active);       // 引擎层：憋压报警已升（PRESS02 即时档）
            Assert.Equal(AlarmDemoRig.PressTag, active.Tag);

            var row = Assert.Single(vm.MainRows);                // 呈现层：同一行直译进主列表
            Assert.Equal(AlarmDemoRig.PressTag, row.Tag);
        }

        /// <summary>档案就位幂等（#22）：同一档案库二度通电——缺了才补，不撞位号不重播。</summary>
        [Fact]
        public void 二度通电_档案缺了才补_不炸不重复()
        {
            var store = new InMemoryAlarmDefinitionStore();
            _ = new AlarmDemoRig(store);
            _ = new AlarmDemoRig(store); // 第二台装置同一档案库：档案已有的位号不再补证

            Assert.Equal(3, store.LoadAll().Count);
        }

        /// <summary>通电落库（#22）：接了事件史，演示的每一拍全量进本子——五拍憋压即时档升的那一次，库里就有。
        /// 落库走后台冲账（#31），断言前经装置冲账：存量清完才对账。</summary>
        [Fact]
        public void 接通事件库_剧本五拍_事件全量落本子()
        {
            var events = new RecordingEventStore();
            var rig = new AlarmDemoRig(new InMemoryAlarmDefinitionStore(), events);
            var vm = new AlarmListViewModel(rig.Engine, rig.Store, new SyncContext());

            for (var beat = 0; beat < 5; beat++)
            {
                rig.PumpNext();
                vm.RefreshFilters();
            }

            rig.History?.Dispose();                              // 冲账：后台写者清完存量再断言（#31）

            var record = Assert.Single(events.Events);           // 引擎升几次，本子记几笔：一笔不缺
            var raised = Assert.IsType<AlarmRaised>(record);
            Assert.Equal(AlarmDemoRig.PressTag, raised.Tag);     // 憋压即时档（其余两位延时未满）
        }

        /// <summary>装置收口冲账（#31 评审）：Dispose 先冲账再退订采集层——收口那一刻队列里的
        /// 尾巴要进本子，宿主正常关屏不许丢账（崩溃窗口才是 ADR-0011 认的丢账口）。</summary>
        [Fact]
        public void 装置收口_先冲账再退订_本子不欠账()
        {
            var events = new RecordingEventStore();
            var rig = new AlarmDemoRig(new InMemoryAlarmDefinitionStore(), events);
            var vm = new AlarmListViewModel(rig.Engine, rig.Store, new SyncContext());

            for (var beat = 0; beat < 5; beat++)
            {
                rig.PumpNext();
                vm.RefreshFilters();
            }

            rig.Dispose();                                       // 收口：冲账在前，退订在后

            var record = Assert.Single(events.Events);           // 队列尾巴进了本子：一笔不缺
            Assert.Equal(AlarmDemoRig.PressTag, Assert.IsType<AlarmRaised>(record).Tag);
        }
    }
}
