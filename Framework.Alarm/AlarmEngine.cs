
namespace Framework.Alarm
{
    /// <summary>
    /// 报警引擎（摘要 §8.1–8.2）：全系统唯一「懂报警」的地方——条件事实进 → 查档案 →
    /// 死区/延时滤波（<see cref="HysteresisEvaluator"/>）→ 四状态机（<see cref="AlarmStateMachine"/>）流转 →
    /// 产出 <see cref="AlarmEvent"/> 族 + 活跃列表读模型。纯逻辑、无 UI 依赖。
    /// 两条沉默决策（哲学 §3.4【强制】）：无档案不报警、报警中不重播。
    /// 搁置 = 事件打标（<c>ShelvedUntil</c>，摘要 §8.10，哲学 §7【强制】）：不拦状态机，
    /// 搁置期间条件照常触发、事件照常落库，只是带标——呈现按标过滤，到期标自然失效。
    /// 搁置登记是事件流的投影（#16）：总装时若接了历史库（<paramref name="eventStore"/>），
    /// 按落库先后回放搁置/解除事件重建登记本——重启、改档重建都不丢操作员的搁置单；
    /// 回放是读历史，不是改历史（append-only 铁律不受触碰）。
    /// 每条报警一个频道：滤波器组态来自档案（组态参数来自档案，哲学 §6.1）。
    /// 档案启动时全量加载，运行期只读（档案驱动，不在 if 里长报警）；改档后重建引擎接新档案，
    /// 旧实例 <see cref="Dispose"/> 退订采集层，避免双引擎重复播报。
    /// 线程模型：采集层线程喂事实、UI 线程查询/确认/搁置——入口全程加锁，活跃列表返回锁内快照；
    /// 订阅方跨线程编队归呈现层（摘要 §9.2）。
    /// </summary>
    public sealed class AlarmEngine : IAlarmFeed, IDisposable
    {
        private readonly object _gate = new();                  // 事实进、查询、确认、搁置四口的锁
        private readonly IAlarmClock _clock;
        private readonly IConditionSource _source;
        private readonly Dictionary<string, Channel> _channels; // 条件事实按位号找到自己的频道
        private readonly List<ActiveAlarm> _active = new();     // 活跃列表：事件流的投影，不是第二份真相
        private readonly Dictionary<string, DateTimeOffset> _shelved = new(); // 搁置登记：位号 → 期限（摘要 §8.10 的标）

        /// <summary>回放点名簿（#18）：登记本只由搁置/解除两种事件写成，其余判别留在库里不物化。</summary>
        private static readonly Type[] ShelveReplayKinds = [typeof(AlarmShelved), typeof(AlarmUnshelved)];

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

        /// <summary>事件流出口：落历史库、推 UI 都从这里订阅（订阅即落库，一行接线，摘要 §8.11）。
        /// 订阅方必须异步编队（Post/BeginInvoke）回自己的线程——引擎在锁内同步播报，同步等待 UI
        /// （Invoke/.Result 等）即死锁；跨线程编队归呈现层（摘要 §9.2）。</summary>
        public event Action<AlarmEvent>? EventRaised;

        /// <summary>活跃列表只读投影：成员 = 未确认 + 已确认 + RTN；按活跃起点先后排列（排序置顶按档案优先级是呈现层的事）。
        /// 搁置标在快照读取时对表打上（<see cref="ShelvedUntilOf"/>）——时钟一过标自然失效，
        /// 到期不用定时器解除（摘要 §8.10）；主列表/搁置清单的过滤见 <see cref="AlarmFilters"/>。</summary>
        public IReadOnlyList<ActiveAlarm> Active
        {
            get
            {
                lock (_gate)
                {
                    var now = _clock.Now;                      // 一张快照对一次表：全体成员同一时刻的真相
                    return _active
                        .OrderBy(a => a.Since)                 // 锁内快照：枚举期间列表不许被人改
                        .Select(a => a with { ShelvedUntil = ShelvedUntilOf(a.Tag, now) })
                        .ToList();
                }
            }
        }

        /// <summary>总装：接上采集层（订阅条件事实）、装上档案（全量加载，档案驱动）、对好表（时钟接缝）；
        /// 给了历史库则回放搁置登记（<see cref="RestoreShelved"/>，#16）——重启不丢操作员的搁置单。</summary>
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
                        Raise(new AlarmRaised(tag, sample.At, channel.Definition.Priority,
                            ShelvedUntil: ShelvedUntilOf(tag, sample.At))); // 投影先行，事件后发；搁置中升级则带标（摘要 §8.10）
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
        /// RTN → 确认恢复闭环，产 <see cref="AlarmClosedByOperator"/>（操作员闭环留名，#26）并移出列表。
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
                    Raise(new AlarmClosedByOperator(tag, _clock.Now, op)); // 操作员闭环留名（#26）：与条件驱动的闭环分型
                }
            }
        }

        /// <summary>
        /// 操作员搁置入口（摘要 §8.4 <c>IAlarmFeed.Shelve</c>，机制 §8.10，哲学 §7【强制】）：
        /// 搁置 = 事件打标，不拦状态机——只在搁置登记上记一笔期限：条件照常触发、状态照常流转、
        /// 事件照常落库（反面约束：做「搁置开关」拦状态机 → 历史库缺记录 → KPI 体检成瞎子）；
        /// 主列表/搁置清单的按标过滤是呈现层的事（<see cref="AlarmFilters"/>）。
        /// 搁置必须限时（警示 15）：duration ≤ 0 = 无限期搁置的变体 = 变相删报警，静默拒绝；
        /// 无档案位号同门规静默。时刻取时钟接缝当下，动作全量留痕（产 <see cref="AlarmShelved"/>）；
        /// 重复搁置以最新下单为准。
        /// </summary>
        public void Shelve(string tag, TimeSpan duration, string op)
        {
            if (duration <= TimeSpan.Zero || !_channels.ContainsKey(tag))
            {
                return; // 无效下单与无档案位号：静默——没有动作就不留痕
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
        /// 操作员解除搁置入口（摘要 §8.4 <c>IAlarmFeed.Unshelve</c>）：撤标——下一拍快照起
        /// 报警回主列表正常播报；产 <see cref="AlarmUnshelved"/> 留痕（时刻取时钟接缝当下）。
        /// 没搁置过的位号、已自然到期的订单（标的已失效，报警早回主列表）一律静默——
        /// 没在搁置就无从解除，没有动作就不留假痕；到期的标本就无需解除（摘要 §8.10）。
        /// </summary>
        public void Unshelve(string tag, string op)
        {
            lock (_gate)
            {
                var now = _clock.Now;
                var effective = ShelvedUntilOf(tag, now);
                _shelved.Remove(tag);                          // 过期旧账顺手清掉，登记本不养死单
                if (effective is null)
                {
                    return;                                    // 没在搁置（没登记/已到期）：静默
                }

                Raise(new AlarmUnshelved(tag, now, op));
            }
        }

        /// <summary>
        /// 回放搁置登记（#16）：按落库先后（append-only 本子的书写次序即履历次序）重放
        /// <see cref="AlarmShelved"/>（登记期限）/ <see cref="AlarmUnshelved"/>（撤账）——
        /// 重复搁置以最新下单为准，与运行期同规矩；其余事件种类不动登记本（升级事件的标是读登记本打的，不是写）。
        /// 回放点名搁置两种判别进库筛（#18）：判别名不出存储接口，引擎点 CLR 类型——
        /// 库大即顿的解药是把筛下推进库，不把整本子物化进内存再扔。
        /// 回放完对表清旧账：已到期的订单不复活（时钟一过标自然失效，摘要 §8.10），档案已删的位号不进新登记本。
        /// append-only 边界：只读不写——回放期间不追加、不吐事件。
        /// 历史库启动期读不出来：登记回不来的后果是搁置失效回主列表（宁多报不漏报），不许挡引擎总装。
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
                // 历史库故障：空登记起步（fail toward alarming），故障归历史库通道自证。
                return;
            }

            var now = _clock.Now;
            foreach (var tag in _shelved.Where(kv => kv.Value <= now || !_channels.ContainsKey(kv.Key)).Select(kv => kv.Key).ToList())
            {
                _shelved.Remove(tag); // 旧账与死单不进新登记本
            }
        }

        /// <summary>位号在 moment 时刻的搁置标：期限还在 moment 之后则带标，否则 null——
        /// 时钟一过标自然失效（摘要 §8.10），登记里过期的旧账从此无人问津，不设清理定时器。
        /// 升级打标对的是事实时间戳（事件自身时刻自洽），活跃快照对的是时钟当下（读取时刻的真相）。</summary>
        private DateTimeOffset? ShelvedUntilOf(string tag, DateTimeOffset moment)
            => _shelved.TryGetValue(tag, out var until) && until > moment ? until : null;

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

        /// <summary>事件出口：订阅方（历史库/UI）从这收，引擎不知道订阅方是谁。
        /// 订阅方异常不回传引擎（锁内播报：一家落库失败不许拖垮事实流、更不许留下投影与历史各说各话的
        /// 半拍——落库重试是订阅方自己的职责，引擎只保证条件事实不停摆）。</summary>
        private void Raise(AlarmEvent e)
        {
            try
            {
                EventRaised?.Invoke(e);
            }
            catch
            {
                // 吞：订阅方（历史库/UI）的故障停在订阅方；事实与投影已先行落定（投影先行，事件后发）。
            }
        }
    }
}
