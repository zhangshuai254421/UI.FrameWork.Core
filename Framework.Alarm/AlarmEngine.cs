
namespace Framework.Alarm
{
    /// <summary>
    /// 报警引擎（摘要 §8.1–8.2）：全系统唯一「懂报警」的地方——条件事实进 → 查档案 →
    /// 死区/延时滤波（<see cref="HysteresisEvaluator"/>）→ 四状态机（<see cref="AlarmStateMachine"/>）流转 →
    /// 产出 <see cref="AlarmEvent"/> 族 + 活跃列表读模型。纯逻辑、无 UI 依赖。
    /// 两条沉默决策（哲学 §3.4【强制】）：无档案不报警、报警中不重播。
    /// 每条报警一个频道：滤波器组态来自档案（组态参数来自档案，哲学 §6.1）。
    /// 档案启动时全量加载，运行期只读（档案驱动，不在 if 里长报警）；改档后重建引擎接新档案，
    /// 旧实例 <see cref="Dispose"/> 退订采集层，避免双引擎重复播报。
    /// 线程模型：采集层线程喂事实、UI 线程查询/确认——入口全程加锁，活跃列表返回锁内快照；
    /// 订阅方跨线程编队归呈现层（摘要 §9.2）。
    /// </summary>
    public sealed class AlarmEngine : IAlarmFeed, IDisposable
    {
        private readonly object _gate = new();                  // 事实进、查询、确认三口的锁
        private readonly IAlarmClock _clock;
        private readonly IConditionSource _source;
        private readonly Dictionary<string, Channel> _channels; // 条件事实按位号找到自己的频道
        private readonly List<ActiveAlarm> _active = new();     // 活跃列表：事件流的投影，不是第二份真相

        /// <summary>一条报警的频道：档案（判定依据）+ 滤波器（何时够格）+ 状态机（被人处理到哪一步），状态私有。</summary>
        private sealed class Channel
        {
            public Channel(AlarmDefinition definition)
            {
                Definition = definition;
                Evaluator = new HysteresisEvaluator(
                    definition.Limit, definition.Deadband, definition.OnDelay, definition.OffDelay);
            }

            public AlarmDefinition Definition { get; }
            public HysteresisEvaluator Evaluator { get; }
            public AlarmStateMachine Machine { get; } = new();
        }

        /// <summary>事件流出口：落历史库、推 UI 都从这里订阅（订阅即落库，一行接线，摘要 §8.11）。</summary>
        public event Action<AlarmEvent>? EventRaised;

        /// <summary>活跃列表只读投影：成员 = 未确认 + 已确认 + RTN；按活跃起点先后排列（排序置顶按档案优先级是呈现层的事）。</summary>
        public IReadOnlyList<ActiveAlarm> Active
        {
            get
            {
                lock (_gate)
                {
                    return _active.OrderBy(a => a.Since).ToList(); // 锁内快照：枚举期间列表不许被人改
                }
            }
        }

        /// <summary>总装：接上采集层（订阅条件事实）、装上档案（全量加载，档案驱动）、对好表（时钟接缝）。</summary>
        public AlarmEngine(IConditionSource source, IAlarmDefinitionStore store, IAlarmClock clock)
        {
            _source = source;
            _clock = clock;
            _channels = store.LoadAll().ToDictionary(d => d.Tag, d => new Channel(d));
            source.ConditionChanged += OnConditionChanged;
        }

        /// <summary>退订采集层：重建引擎换档案后处置旧实例，不留下重复播报的幽灵引擎。</summary>
        public void Dispose() => _source.ConditionChanged -= OnConditionChanged;

        /// <summary>
        /// 条件事实进引擎：查档案 → 滤波 → 状态机 → 事件。无档案直接忽略（沉默决策一：
        /// 先立档案再报警，哲学 §3.4）；滤波静默（延时未满、死区带内……）也不产事件——寂静也是断言。
        /// </summary>
        private void OnConditionChanged(string tag, ConditionSample sample)
        {
            if (!_channels.TryGetValue(tag, out var channel))
            {
                return; // 无档案不报警——越限再离谱也没有出生证
            }

            lock (_gate)
            {
                var before = channel.Machine.CurrentState;
                var decision = channel.Evaluator.Evaluate(sample);

                switch (decision)
                {
                    case FilterDecision.Raise:
                        channel.Machine.Trigger(); // 报警中（未确认/已确认）不重播；RTN 再激活（状态机补充转换）
                        break;
                    case FilterDecision.Clear:
                        channel.Machine.Clear();   // 已确认直接回 Normal，未确认进 RTN（RTN 口径保真项）
                        break;
                }

                var after = channel.Machine.CurrentState;
                if (after == before)
                {
                    return; // 转换没发生（重播/非法时序）＝静默，不产事件
                }

                switch (after)
                {
                    case AlarmState.Unacknowledged: // Normal → 未确认：闪红起；或 RTN 再激活：闪绿变回闪红
                        Upsert(tag, new ActiveAlarm(tag, after, channel.Definition.Priority, Since: sample.At));
                        Raise(new AlarmRaised(tag, sample.At, channel.Definition.Priority)); // 投影先行，事件后发
                        break;
                    case AlarmState.ReturnToNormal: // 确认之前条件自愈：无人看见的恢复，闪绿留列表
                        ReplaceState(tag, after);
                        Raise(new AlarmAutoReturned(tag, sample.At));
                        break;
                    case AlarmState.Normal: // 已确认后条件恢复：有主人的报警，回家不用再敲门
                        _active.RemoveAll(a => a.Tag == tag);
                        Raise(new AlarmClosed(tag, sample.At));
                        break;
                }
            }
        }

        /// <summary>
        /// 操作员确认入口（摘要 §8.4 IAlarmFeed.Acknowledge）：一个「看见了」两个去处——
        /// 未确认 → 已确认，产 <see cref="AlarmAcknowledged"/>（责任接管，仍留列表）；
        /// RTN → 确认恢复闭环，产 <see cref="AlarmClosed"/> 并移出列表。
        /// 状态不许可时静默忽略（非法转换契约，ADR-0009 规则 9）。时刻取时钟接缝当下，操作员全量留痕。
        /// </summary>
        public void Acknowledge(string tag, string op)
        {
            if (!_channels.TryGetValue(tag, out var channel))
            {
                return; // 无档案的位号：静默——先立档案再报警
            }

            lock (_gate)
            {
                var machine = channel.Machine;
                var before = machine.CurrentState;

                machine.Acknowledge(); // 未确认 → 已确认；其余状态 no-op
                if (machine.CurrentState != before)
                {
                    ReplaceState(tag, machine.CurrentState);
                    Raise(new AlarmAcknowledged(tag, _clock.Now, op)); // 投影先行，事件后发
                    return;
                }

                machine.AcknowledgeReturn(); // RTN → Normal：等到了那个「看见了」，真闭环；其余状态 no-op
                if (machine.CurrentState != before)
                {
                    _active.RemoveAll(a => a.Tag == tag);
                    Raise(new AlarmClosed(tag, _clock.Now));
                }
            }
        }

        /// <summary>投影上播：升级入列；RTN 再激活则原位刷新（状态回未确认、活跃起点刷新为新一段的升级时刻）。</summary>
        private void Upsert(string tag, ActiveAlarm member)
        {
            var index = _active.FindIndex(a => a.Tag == tag);
            if (index >= 0)
            {
                _active[index] = member;
            }
            else
            {
                _active.Add(member);
            }
        }

        /// <summary>投影修正：报警还挂着但状态变了（未确认 → 已确认 / RTN）。</summary>
        private void ReplaceState(string tag, AlarmState state)
        {
            var index = _active.FindIndex(a => a.Tag == tag);
            if (index >= 0)
            {
                _active[index] = _active[index] with { State = state };
            }
        }

        /// <summary>事件出口：订阅方（历史库/UI）从这收，引擎不知道订阅方是谁。</summary>
        private void Raise(AlarmEvent e) => EventRaised?.Invoke(e);
    }
}
