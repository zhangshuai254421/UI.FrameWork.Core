using Framework.Alarm;
using Framework.Alarm.Domain;
using Prism.Mvvm;

namespace Framework.Alarm.UI
{
    /// <summary>
    /// 报警行（摘要 §9.3）：<see cref="ActiveAlarm"/> 的可绑定包装——引擎产出的字段原样持有
    /// （三态样式绑 State，不按数值差算；排序按档案 Priority），由 <see cref="AlarmListViewModel"/>
    /// 按事件原位刷新（同一行实例就地更新，列表不闪、选中不丢）。
    /// </summary>
    public sealed class AlarmRow : BindableBase
    {
        /// <summary>状态后端字段。</summary>
        private AlarmState _state;

        /// <summary>活跃起点后端字段。</summary>
        private DateTimeOffset _since;

        /// <summary>搁置期限后端字段。</summary>
        private DateTimeOffset? _shelvedUntil;

        /// <summary>构造：字段全部来自引擎产出（档案/事件），UI 不加工。</summary>
        public AlarmRow(string tag, AlarmState state, AlarmPriority priority, DateTimeOffset since, DateTimeOffset? shelvedUntil)
        {
            Tag = tag;
            _state = state;
            Priority = priority;
            _since = since;
            _shelvedUntil = shelvedUntil;
        }

        /// <summary>从活跃快照造行（<see cref="AlarmListViewModel.Reload"/> 还原用）。</summary>
        public AlarmRow(ActiveAlarm member)
            : this(member.Tag, member.State, member.Priority, member.Since, member.ShelvedUntil)
        {
        }

        /// <summary>位号。</summary>
        public string Tag { get; }

        /// <summary>报警状态：三态样式绑它（未确认 = 闪红 / 已确认 = 稳红 / RTN = 闪绿），不按数值差算。</summary>
        public AlarmState State { get => _state; private set => SetProperty(ref _state, value); }

        /// <summary>优先级：来自档案，置顶排序按它。</summary>
        public AlarmPriority Priority { get; }

        /// <summary>活跃起点：本次升级时刻（RTN 再激活时随事件刷新）。</summary>
        public DateTimeOffset Since { get => _since; private set => SetProperty(ref _since, value); }

        /// <summary>搁置期限：还没到期就有值（主列表/搁置清单按它切分，到期由 AlarmFilters 用当前时刻判定失效）。</summary>
        public DateTimeOffset? ShelvedUntil { get => _shelvedUntil; private set => SetProperty(ref _shelvedUntil, value); }

        /// <summary>投影回引擎形状（作为 AlarmFilters 的输入）。</summary>
        public ActiveAlarm ToActiveAlarm() => new(Tag, State, Priority, Since, ShelvedUntil);

        /// <summary>状态事件的原位修正（Acknowledged / AutoReturned）。</summary>
        public void SetState(AlarmState state) => State = state;

        /// <summary>搁置事件的原位修正（Shelved 带新期限 / Unshelved 传 null 表示取消）。</summary>
        public void SetShelvedUntil(DateTimeOffset? shelvedUntil) => ShelvedUntil = shelvedUntil;

        /// <summary>升级事件的原位修正（首次升级造新行，RTN 再激活走这里）：状态回未确认、
        /// 起点和搁置期限照事件刷新——行实例不换（列表不闪，选中与详情弹窗不脱钩）。</summary>
        public void Reraise(DateTimeOffset since, DateTimeOffset? shelvedUntil)
        {
            State = AlarmState.Unacknowledged;
            Since = since;
            ShelvedUntil = shelvedUntil;
        }
    }
}
