
namespace Framework.Alarm
{
    /// <summary>
    /// 报警引擎：全系统唯一理解报警规则的地方（摘要 §8.1–8.2）。处理流程：条件事实进来，
    /// 先查档案，再做死区和延时滤波（<see cref="HysteresisEvaluator"/>），然后四状态机流转
    /// （<see cref="AlarmStateMachine"/>），最后产出 <see cref="AlarmEvent"/> 事件和活跃列表。纯逻辑，不依赖 UI。
    /// 两条沉默规则（哲学 §3.4【强制】）：没有档案就不报警；报警已经存在就不重复发事件。
    /// 搁置的实现方式是给事件和列表快照附加一个期限字段（ShelvedUntil，摘要 §8.10，哲学 §7【强制】）。
    /// 搁置不阻止状态机：搁置期间条件照常触发、事件照常入库，由显示层按期限字段过滤，到期后字段自然失效。
    /// 搁置登记可以从历史库恢复（#16）：构造时传入 eventStore，就按入库顺序回放搁置和解除事件，
    /// 重建登记表。这样重启或换档案重建引擎后，操作员的搁置单不会丢。回放只读历史，不写历史（append-only 铁律不受影响）。
    /// 每条报警对应一个独立的通道对象（档案 + 滤波器 + 状态机），滤波参数来自档案（哲学 §6.1）。
    /// 档案在构造时全部加载，运行期只读；修改档案后应重建引擎来接新档案，
    /// 旧引擎调用 <see cref="Dispose"/> 退订采集层，避免两个引擎重复发事件。
    /// 线程模型：采集层线程送事实，UI 线程查询、确认、搁置。所有公共入口共用一把锁，
    /// 活跃列表返回锁内快照；订阅方收到事件后回到哪个线程，由订阅方自己投递（摘要 §9.2）。
    /// </summary>
    public sealed class AlarmEngine : IAlarmFeed, IDisposable
    {
        private readonly object _gate = new();                  // 所有公共入口共用的锁：送事实、查询、确认、搁置都走它
        private readonly IAlarmClock _clock;
        private readonly IConditionSource _source;
        private readonly Dictionary<string, Channel> _channels; // 位号 → 该报警的通道（档案 + 滤波器 + 状态机）
        private readonly List<ActiveAlarm> _active = new();     // 活跃列表：事件流的投影，不是第二份真相
        private readonly Dictionary<string, DateTimeOffset> _shelved = new(); // 搁置登记表：位号 → 期限（摘要 §8.10）

        /// <summary>回放搁置登记时只查询这两种事件类型（#18）：登记表只由搁置和解除事件写成，
        /// 其他类型不必从库里读出来。</summary>
        private static readonly Type[] ShelveReplayKinds = [typeof(AlarmShelved), typeof(AlarmUnshelved)];

        /// <summary>一条报警的通道：档案（判定依据）、滤波器（何时够格报警）、状态机（处理进行到哪一步）。</summary>
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

        /// <summary>事件出口：历史库、UI 都从这里订阅（订阅即落库，一行接线，摘要 §8.11）。
        /// 引擎在锁内同步触发本事件，订阅方若在其他线程更新界面，必须用 Post 或 BeginInvoke 投递回去，
        /// 禁止同步等待（Invoke、.Result 等），否则死锁（摘要 §9.2）。</summary>
        public event Action<AlarmEvent>? EventRaised;

        /// <summary>活跃列表的只读快照：成员是未确认、已确认和 RTN 的报警，按活跃起点先后排列
        /// （按优先级置顶排序是显示层自己的事）。每次读取时用当前时刻补算各成员的搁置期限
        /// （<see cref="ShelvedUntilOf"/>），期限一过自然失效，所以到期不需要定时器（摘要 §8.10）。
        /// 主列表和搁置清单的过滤规则在 <see cref="AlarmFilters"/>。</summary>
        public IReadOnlyList<ActiveAlarm> Active
        {
            get
            {
                lock (_gate)
                {
                    var now = _clock.Now;                      // 一次读取统一用一个时刻，全体成员一致
                    return _active
                        .OrderBy(a => a.Since)                 // 锁内完成排序和复制，枚举期间列表不会被并发修改
                        .Select(a => a with { ShelvedUntil = ShelvedUntilOf(a.Tag, now) })
                        .ToList();
                }
            }
        }

        /// <summary>总装：接上采集层（订阅条件事实）、加载全部档案、注入时钟；
        /// 传入 eventStore 则回放搁置登记（<see cref="RestoreShelved"/>，#16），重启后搁置单不丢。</summary>
        public AlarmEngine(IConditionSource source, IAlarmDefinitionStore store, IAlarmClock clock, IEventStore? eventStore = null)
        {
            _source = source;
            _clock = clock;
            _channels = store.LoadAll().ToDictionary(d => d.Tag, d => new Channel(d));
            if (eventStore is not null)
            {
                RestoreShelved(eventStore);
            }

            source.ConditionChanged += OnConditionChanged;
        }

        /// <summary>退订采集层。重建引擎换档案后调用，处置旧实例，避免两个引擎重复发事件。</summary>
        public void Dispose() => _source.ConditionChanged -= OnConditionChanged;

        /// <summary>
        /// 条件事实进入引擎：查档案 → 滤波 → 状态机 → 事件。
        /// 没有档案的位号直接忽略（沉默规则一：先立档案再报警，哲学 §3.4）；
        /// 滤波判定为静默（延时未满、死区带内等）同样不产事件，不发事件也是引擎的正式输出。
        /// </summary>
        private void OnConditionChanged(string tag, ConditionSample sample)
        {
            if (!_channels.TryGetValue(tag, out var channel))
            {
                return; // 没有档案的位号：静默
            }

            lock (_gate)
            {
                var before = channel.Machine.CurrentState;
                var decision = channel.Evaluator.Evaluate(sample);

                switch (decision)
                {
                    case FilterDecision.Raise:
                        channel.Machine.Trigger(); // 报警中再触发是无效转换，状态机内部忽略；RTN 时再触发则是再激活
                        break;
                    case FilterDecision.Clear:
                        channel.Machine.Clear();   // 已确认时回到 Normal（直接闭环），未确认时进 RTN
                        break;
                }

                var after = channel.Machine.CurrentState;
                if (after == before)
                {
                    return; // 状态没变（重复报值或非法时序）：不发事件
                }

                switch (after)
                {
                    case AlarmState.Unacknowledged: // 首次升级；或 RTN 后再次越限（再激活）
                        Upsert(tag, new ActiveAlarm(tag, after, channel.Definition.Priority, Since: sample.At));
                        Raise(new AlarmRaised(tag, sample.At, channel.Definition.Priority,
                            ShelvedUntil: ShelvedUntilOf(tag, sample.At))); // 先更新活跃列表再发事件；搁置中升级则把当前期限附在事件上（摘要 §8.10）
                        break;
                    case AlarmState.ReturnToNormal: // 确认之前条件自行恢复：进 RTN，保留在列表等操作员确认
                        ReplaceState(tag, after);
                        Raise(new AlarmAutoReturned(tag, sample.At));
                        break;
                    case AlarmState.Normal: // 已确认后条件恢复：直接闭环，从活跃列表移除
                        _active.RemoveAll(a => a.Tag == tag);
                        Raise(new AlarmClosed(tag, sample.At));
                        break;
                }
            }
        }

        /// <summary>
        /// 操作员确认（摘要 §8.4 IAlarmFeed.Acknowledge）：一个「看见了」有两个去处。
        /// 未确认 → 已确认：发 <see cref="AlarmAcknowledged"/>（责任接管，报警仍在列表）；
        /// RTN → 闭环：发 <see cref="AlarmClosedByOperator"/>（操作员确认闭环，#26），移出列表。
        /// 状态不允许确认时静默忽略（非法转换契约，ADR-0009 规则 9）。
        /// 时刻取时钟当前值，操作员名随事件留痕。
        /// </summary>
        public void Acknowledge(string tag, string op)
        {
            if (!_channels.TryGetValue(tag, out var channel))
            {
                return; // 没有档案的位号：静默
            }

            lock (_gate)
            {
                var machine = channel.Machine;
                var before = machine.CurrentState;

                machine.Acknowledge(); // 未确认 → 已确认；其余状态无效
                if (machine.CurrentState != before)
                {
                    ReplaceState(tag, machine.CurrentState);
                    Raise(new AlarmAcknowledged(tag, _clock.Now, op)); // 先更新活跃列表再发事件
                    return;
                }

                machine.AcknowledgeReturn(); // RTN → Normal：确认恢复闭环；其余状态无效
                if (machine.CurrentState != before)
                {
                    _active.RemoveAll(a => a.Tag == tag);
                    Raise(new AlarmClosedByOperator(tag, _clock.Now, op)); // 操作员确认闭环单独分型（#26），与条件恢复的闭环区分
                }
            }
        }

        /// <summary>
        /// 操作员搁置（摘要 §8.4 <c>IAlarmFeed.Shelve</c>，机制 §8.10，哲学 §7【强制】）：
        /// 只在搁置登记表记一个期限，不阻止状态机。条件照常触发、状态照常流转、事件照常入库。
        /// 反面约束：如果做成「搁置开关」阻止状态机，历史库会缺记录，KPI 统计就失真了。
        /// 期限必须大于零（警示 15）：无限期搁置等于变相删除报警，所以静默拒绝；无档案位号同样静默。
        /// 时刻取时钟当前值，动作发 <see cref="AlarmShelved"/> 留痕；重复搁置以最后一次为准。
        /// </summary>
        public void Shelve(string tag, TimeSpan duration, string op)
        {
            if (duration <= TimeSpan.Zero || !_channels.ContainsKey(tag))
            {
                return; // 期限无效或没有档案：静默，没有动作就不留痕
            }

            lock (_gate)
            {
                var now = _clock.Now;
                var until = now + duration;
                _shelved[tag] = until;
                Raise(new AlarmShelved(tag, now, until, op));
            }
        }

        /// <summary>
        /// 操作员解除搁置（摘要 §8.4 <c>IAlarmFeed.Unshelve</c>）：删除登记表里的期限，
        /// 下次读取快照起报警回到主列表；发 <see cref="AlarmUnshelved"/> 留痕（时刻取时钟当前值）。
        /// 从没搁置过、或期限已自然到期的位号一律静默：没有可解除的搁置，就不留事件（摘要 §8.10）。
        /// </summary>
        public void Unshelve(string tag, string op)
        {
            lock (_gate)
            {
                var now = _clock.Now;
                var effective = ShelvedUntilOf(tag, now);
                _shelved.Remove(tag);                          // 顺手删掉已过期的记录，登记表不留死数据
                if (effective is null)
                {
                    return;                                    // 没在搁置（没登记或已到期）：静默
                }

                Raise(new AlarmUnshelved(tag, now, op));
            }
        }

        /// <summary>
        /// 回放搁置登记（#16）：按入库顺序重放搁置事件（记期限）和解除事件（删记录），重建登记表。
        /// 重复搁置以最后一次为准，与运行期一致；其他事件类型不影响登记表。
        /// 查询只筛这两种类型，让库自己去筛（#18）：库大时如果把全部历史读进内存再扔掉，会卡住启动。
        /// 回放完成后清理：已到期的期限不恢复（时钟比较自然失效，摘要 §8.10），档案已删除的位号不进登记表。
        /// append-only 边界：回放只读，不追加、不发事件。
        /// 历史库读不出来（故障）时以空登记表启动：搁置失效的后果是报警回到主列表，宁可多报不漏报，也不阻塞引擎构造。
        /// </summary>
        private void RestoreShelved(IEventStore eventStore)
        {
            try
            {
                foreach (var e in eventStore.Query(
                    DateTimeOffset.MinValue, DateTimeOffset.MaxValue, kinds: ShelveReplayKinds))
                {
                    switch (e)
                    {
                        case AlarmShelved shelved:
                            _shelved[shelved.Tag] = shelved.ShelvedUntil;
                            break;
                        case AlarmUnshelved:
                            _shelved.Remove(e.Tag);
                            break;
                    }
                }
            }
            catch
            {
                // 历史库故障：以空登记表启动（宁可多报，不漏报），故障归历史库通道自己排查
                return;
            }

            var now = _clock.Now;
            foreach (var tag in _shelved.Where(kv => kv.Value <= now || !_channels.ContainsKey(kv.Key)).Select(kv => kv.Key).ToList())
            {
                _shelved.Remove(tag); // 已到期或档案已删的记录不进登记表
            }
        }

        /// <summary>返回位号在指定时刻的搁置期限：还没到期就返回期限，已到期返回 null。
        /// 过期的搁置不需要清理：每次查询都拿当前时刻比较，过期记录永远查不中，所以不设定时器
        /// （设计依据：摘要 §8.10）。注意两处调用的时刻不同：升级事件附期限用事件自身的时间戳
        /// （保证事件记录自洽），活跃列表快照用当前时钟（表示此刻状态），两者不能混用。</summary>
        private DateTimeOffset? ShelvedUntilOf(string tag, DateTimeOffset moment)
            => _shelved.TryGetValue(tag, out var until) && until > moment ? until : null;

        /// <summary>更新活跃列表成员：已存在则原位替换（RTN 再激活，状态回未确认、活跃起点刷新），不存在则加入（首次升级）。</summary>
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

        /// <summary>修改活跃成员的状态（未确认 → 已确认 / RTN），位号不变。</summary>
        private void ReplaceState(string tag, AlarmState state)
        {
            var index = _active.FindIndex(a => a.Tag == tag);
            if (index >= 0)
            {
                _active[index] = _active[index] with { State = state };
            }
        }

        /// <summary>触发事件出口。订阅方（历史库/UI）抛出的异常在此吞掉、不回传引擎：
        /// 此时事实已处理、活跃列表已更新，一家订阅方故障不能拖垮事实流；
        /// 落库失败的重试是订阅方（EventHistory）自己的职责。</summary>
        private void Raise(AlarmEvent e)
        {
            try
            {
                EventRaised?.Invoke(e);
            }
            catch
            {
                // 订阅方故障不上抛：事实与活跃列表已经更新完毕
            }
        }
    }
}
