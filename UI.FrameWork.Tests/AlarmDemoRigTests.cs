using Framework.Alarm;
using Framework.Alarm.Domain;

namespace SemiAppliaction.Views.Tests
{
    /// <summary>
    /// 演示装置直通联测：真引擎（AlarmDemoRig）→ 真 AlarmListViewModel，中间不经任何替身——
    /// 单层契约测试各绿不等于接线通（页面「两清单一直空」的排查钉子）：引擎升没升（Active）、
    /// 呈现接没接住（MainRows）一层一个断言，断点在哪一层当场现形。
    /// </summary>
    public class AlarmDemoRigTests
    {
        /// <summary>同步编队替身：Post 即执行（与 AlarmListViewModelTests 同款）。</summary>
        private sealed class SyncContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object? state) => d(state);
        }

        /// <summary>
        /// 真装置直通：剧本推五拍（憋压即时触发档）——引擎 Active 里得有报警，
        /// 主列表里也得有同一行。两清单一直空的病根要么在引擎层（Active 空），要么在直译层（MainRows 空）。
        /// </summary>
        [Fact]
        public void 真装置直通_剧本五拍_引擎升了主列表接住()
        {
            var rig = new AlarmDemoRig();
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
    }
}
