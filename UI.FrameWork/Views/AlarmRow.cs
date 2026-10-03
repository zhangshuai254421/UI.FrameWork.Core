using Framework.Alarm;
using Prism.Mvvm;

namespace SemiAppliaction.Views
{
    /// <summary>
    /// 报警行（摘要 §9.3）：<see cref="ActiveAlarm"/> 的可绑定包装——引擎产出的字段原样持有
    /// （三态样式绑 State，不绑数值差；排序按档案 Priority），由 <see cref="AlarmListViewModel"/>
    /// 按事件原位刷新（同一行实例更新，列表不闪、选中不丢）。
    /// </summary>
    public sealed class AlarmRow : BindableBase
    {
        /// <summary>状态后端字段。</summary>
        private AlarmState _state;

        /// <summary>搁置标后端字段。</summary>
        private DateTimeOffset? _shelvedUntil;

        /// <summary>构造：字段全部来自引擎产出（档案/事件），UI 不加工。</summary>
        public AlarmRow(string tag, AlarmState state, AlarmPriority priority, DateTimeOffset since, DateTimeOffset? shelvedUntil)
        {
            Tag = tag;
            _state = state;
            Priority = priority;
            Since = since;
            _shelvedUntil = shelvedUntil;
        }

        /// <summary>从活跃快照造行（<see cref="AlarmListViewModel.Reload"/> 还原用）。</summary>
        public AlarmRow(ActiveAlarm member)
            : this(member.Tag, member.State, member.Priority, member.Since, member.ShelvedUntil)
        {
        }

        /// <summary>位号。</summary>
        public string Tag { get; }

        /// <summary>报警状态：三态样式绑它（未确认=闪红 / 已确认=稳红 / RTN=闪绿），不绑数值差。</summary>
        public AlarmState State { get => _state; private set => SetProperty(ref _state, value); }

        /// <summary>优先级：来自档案，排序置顶按它。</summary>
        public AlarmPriority Priority { get; }

        /// <summary>活跃起点：本次升级时刻。</summary>
        public DateTimeOffset Since { get; }

        /// <summary>搁置标：期限未过则带标（主列表/搁置清单按它切分，到期由 AlarmFilters 对表失效）。</summary>
        public DateTimeOffset? ShelvedUntil { get => _shelvedUntil; private set => SetProperty(ref _shelvedUntil, value); }

        /// <summary>投影回引擎形状（喂 AlarmFilters 的输入）。</summary>
        public ActiveAlarm ToActiveAlarm() => new(Tag, State, Priority, Since, ShelvedUntil);

        /// <summary>状态事件的原位修正（Acknowledged / AutoReturned）。</summary>
        public void SetState(AlarmState state) => State = state;

        /// <summary>搁置事件的原位修正（Shelved 带新标 / Unshelved 撤标传 null）。</summary>
        public void SetMark(DateTimeOffset? shelvedUntil) => ShelvedUntil = shelvedUntil;
    }
}
