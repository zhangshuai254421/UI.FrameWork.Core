namespace Framework.Alarm
{
    /// <summary>
    /// 死区 + 触发/恢复延时滤波器（报警哲学 §6.2【强制】）：判定条件事实何时够格升级为「该报警了」。
    /// 每条报警一个实例（状态私有）；时间一律用条件事实自带的时间戳，不用本地时钟（ADR-0009 规则 6）。
    /// 机制：触发点 = 限值；恢复点 = 限值 − 死区。越限持续满触发延时才升级，
    /// 回到恢复点以下持续满恢复延时才清除。
    /// 一条例外（#17 盲区修正）：触发延时为零时，第一条越限事实直接升级——延时为零没有「累计」可言；
    /// 带延时的档案第一条照旧只记录起点（没有前一条事实，无法认定「持续」）。
    /// 前提：事实时间戳单调不倒流；乱序事实（早于上一条）忽略不计，累计时长不倒拨。
    /// 两条铁律由组态保证（哲学 §6.2）：触发延时 &lt; MTR；死区 ≥ 信号噪声幅值。
    /// </summary>
    public sealed class HysteresisEvaluator
    {
        /// <summary>组态：限值（触发点，来自合理化评审）。</summary>
        public double Limit { get; }

        /// <summary>组态：死区（恢复点 = 限值 − 死区，略大于噪声幅值）。</summary>
        public double Deadband { get; }

        /// <summary>组态：触发延时（略长于毛刺时长，且 &lt; MTR）。</summary>
        public TimeSpan OnDelay { get; }

        /// <summary>组态：恢复延时（条件在限值附近抖动时才需要）。</summary>
        public TimeSpan OffDelay { get; }

        /// <summary>用整定后的组态参数构造滤波器（摘要 §6.4：参数必须有意选择，整定依据记入档案）。</summary>
        public HysteresisEvaluator(double limit, double deadband, TimeSpan onDelay, TimeSpan offDelay)
        {
            Limit = limit;
            Deadband = deadband;
            OnDelay = onDelay;
            OffDelay = offDelay;
        }

        private TimeSpan _overAccum;          // 越限段已累计的时长（未报警时）
        private TimeSpan _clearAccum;         // 回到恢复点以下已累计的时长（报警中）
        private DateTimeOffset? _lastAt;      // 上一条事实的时间戳（累计的基准）
        private bool _alarming;               // 已升级、还没清除
        private bool _violated;               // 越限段进行中（上一条事实也越限）
        private bool _recovered;              // 回落段进行中（上一条事实也回到恢复点以下）

        /// <summary>喂一条条件事实，得滤波决策。</summary>
        public FilterDecision Evaluate(ConditionSample sample)
        {
            if (_lastAt is null)
            {
                _lastAt = sample.At; // 第一条事实没有前序，无从累计；段的起点照常记录
                _violated = sample.Value > Limit;

                if (_violated && OnDelay == TimeSpan.Zero)
                {
                    // 第一条就越限且触发延时为零（#17 盲区修正）：直接升级。
                    // 延时为零没有「累计」可言，单条事实就够格；不等第二条来定界
                    // （若第二条已回限内，这次越限将永远升不了）。
                    _alarming = true;
                    _violated = false; // 越限段已被升级消费
                    return FilterDecision.Raise;
                }

                return FilterDecision.None;
            }

            if (sample.At < _lastAt.Value)
            {
                return FilterDecision.None; // 乱序事实：忽略不计。既不许倒拨累计，也不许清掉进行中的段
            }

            var elapsed = sample.At - _lastAt.Value;
            _lastAt = sample.At;

            if (_alarming)
            {
                // 报警中：回到恢复点以下才开始累计清除时长；弹回死区带内或限值之上则清零（噪声在两线之间不翻转）。
                // 回落段从第一条确认回落的事实起算：上一段间隔属于报警期，不计入恢复时长。
                if (sample.Value <= Limit - Deadband)
                {
                    if (_recovered)
                    {
                        _clearAccum += elapsed;
                    }
                    else
                    {
                        _recovered = true; // 回落段起点：从本条事实起算
                    }

                    if (_clearAccum >= OffDelay)
                    {
                        _alarming = false;
                        _recovered = false;
                        _overAccum = TimeSpan.Zero;
                        _clearAccum = TimeSpan.Zero;
                        return FilterDecision.Clear;
                    }
                }
                else
                {
                    _recovered = false;
                    _clearAccum = TimeSpan.Zero;
                }

                return FilterDecision.None;
            }

            if (sample.Value > Limit)
            {
                // 未报警而越限：越限段从第一条越限事实起算，限内段和回落段的间隔不计入（摘要 §8.7 累计清零）
                if (_violated)
                {
                    _overAccum += elapsed;
                }
                else
                {
                    _violated = true; // 越限段起点
                }

                if (_overAccum >= OnDelay)
                {
                    _alarming = true; // 升级一次就够，不重复升级
                    _violated = false; // 越限段已被升级消费
                    _overAccum = TimeSpan.Zero;
                    return FilterDecision.Raise;
                }
            }
            else
            {
                _violated = false;
                _overAccum = TimeSpan.Zero; // 回到限内：累计清零
            }

            return FilterDecision.None;
        }
    }
}
