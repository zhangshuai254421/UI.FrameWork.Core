using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 死区 + 触发/恢复延时滤波器契约测试（报警哲学 §6.2【强制】）。
    /// 全部时间由条件事实自带的时间戳承载（摘要 §8.7：越限累计用条件自带的时间戳，
    /// 不用引擎本地时钟），测试拨表 = 喂时间戳，不真睡、无 DateTime.Now（ADR-0009 规则 6）。
    /// 示例组态：限值 250℃、死区 2℃（恢复点 248℃）、触发延时 2s、恢复延时 1s。
    /// </summary>
    public class HysteresisEvaluatorTests
    {
        /// <summary>限内采样：静默，无任何决策。</summary>
        [Fact]
        public void 采样_限内_静默()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            var decision = evaluator.Evaluate(new ConditionSample(At: T(0), Value: 200));

            Assert.Equal(FilterDecision.None, decision);
        }

        /// <summary>越限但触发延时未满：静默——寂静也是断言（摘要 §11 警示 19）。响应窗口从越限起算，延时先吃一截（铁律一）。</summary>
        [Fact]
        public void 触发_越限未满延时_静默()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            var first = evaluator.Evaluate(new ConditionSample(At: T(0), Value: 260));
            var second = evaluator.Evaluate(new ConditionSample(At: T(1.9), Value: 261));

            Assert.Equal(FilterDecision.None, first);
            Assert.Equal(FilterDecision.None, second); // 差 0.1 秒满延时，仍静默
        }

        /// <summary>越限持续满触发延时：升级为够格报警，决策落在补满延时的那条事实上。边界：恰好满（≥）即升级。</summary>
        [Fact]
        public void 触发_越限满延时_升级()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            var first = evaluator.Evaluate(new ConditionSample(At: T(0), Value: 260));
            var second = evaluator.Evaluate(new ConditionSample(At: T(2), Value: 261));

            Assert.Equal(FilterDecision.None, first);
            Assert.Equal(FilterDecision.Raise, second); // 0→2s 累计满 2s
        }

        /// <summary>毛刺病灶（哲学 §6.2）：短于触发延时的越限不产生任何升级；回到限内累计清零——
        /// 两段各 1.5s 的毛刺总计 3s，也永远凑不满 2s 的连续延时。</summary>
        [Fact]
        public void 触发_毛刺短于延时_被吞()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            var decisions = new[]
            {
                evaluator.Evaluate(new ConditionSample(At: T(0), Value: 260)),    // 毛刺一起
                evaluator.Evaluate(new ConditionSample(At: T(1.5), Value: 200)),  // 1.5s 后回落：累计清零
                evaluator.Evaluate(new ConditionSample(At: T(3), Value: 261)),    // 毛刺二起
                evaluator.Evaluate(new ConditionSample(At: T(4.5), Value: 205)),  // 1.5s 后再回落
                evaluator.Evaluate(new ConditionSample(At: T(6), Value: 210)),    // 限内
            };

            Assert.All(decisions, d => Assert.Equal(FilterDecision.None, d)); // 全程寂静
        }

        /// <summary>徘徊病灶（哲学 §6.2）：报警中，噪声在恢复点(248)与限值(250)之间怎么抖都不清除——
        /// 「噪声在两线之间怎么抖都不翻转状态」，这是死区治徘徊的原理。</summary>
        [Fact]
        public void 恢复_死区带内徘徊_不清除()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 255)));
            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(2), Value: 256))); // 0→2s 满延时升级

            var wandering = new[]
            {
                evaluator.Evaluate(new ConditionSample(At: T(4), Value: 249.5)),   // 落入死区带
                evaluator.Evaluate(new ConditionSample(At: T(5), Value: 248.3)),   // 贴恢复点但不低于
                evaluator.Evaluate(new ConditionSample(At: T(6), Value: 249.9)),   // 弹回
                evaluator.Evaluate(new ConditionSample(At: T(7), Value: 248.05)),  // 再贴
                evaluator.Evaluate(new ConditionSample(At: T(30), Value: 249.0)),  // 抖 23 秒也不清
            };

            Assert.All(wandering, d => Assert.Equal(FilterDecision.None, d)); // 保持报警，无清除决策
        }

        /// <summary>转换四滤波侧（哲学 §6.2）：深回到恢复点(248)以下持续满恢复延时(1s)，才给清除决策。边界：恰好满即清。</summary>
        [Fact]
        public void 恢复_低于恢复点满延时_清除()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 255)));
            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(2), Value: 256)));

            var during = evaluator.Evaluate(new ConditionSample(At: T(2.5), Value: 245)); // 深回：新一轮恢复从此起算，但未满 1s
            var cleared = evaluator.Evaluate(new ConditionSample(At: T(3.5), Value: 244)); // 2.5→3.5s 满 1s

            Assert.Equal(FilterDecision.None, during);
            Assert.Equal(FilterDecision.Clear, cleared);
        }

        /// <summary>恢复延时为零（大多数报警的组态）：深回到恢复点以下的第一条事实即时清。</summary>
        [Fact]
        public void 恢复_无恢复延时_到点即清()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.Zero);

            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 255)));
            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(2), Value: 256)));

            var cleared = evaluator.Evaluate(new ConditionSample(At: T(2.5), Value: 247.9)); // 深回（< 248）

            Assert.Equal(FilterDecision.Clear, cleared);
        }

        /// <summary>
        /// 必测盲区用例（摘要 §11 警示 5，给盲区立证据）：死区带 (248, 250] 内的真实越限**不会报警**——
        /// 这就是死区换镇定的代价凭证。限值若实际应设在 249，这条曲线永远静默：每个旋钮都要能回答
        /// 「我因此对什么视而不见」（摘要 §6.4）。
        /// </summary>
        [Fact]
        public void 盲区_死区带内真实越限被吞()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            var decisions = new[]
            {
                evaluator.Evaluate(new ConditionSample(At: T(0), Value: 249)),    // 带内「越限」
                evaluator.Evaluate(new ConditionSample(At: T(30), Value: 249.5)), // 持续半分钟
                evaluator.Evaluate(new ConditionSample(At: T(60), Value: 249)),   // 持续一分钟
            };

            Assert.All(decisions, d => Assert.Equal(FilterDecision.None, d)); // 全部被吞——有意为之的盲区
        }

        /// <summary>清除之后再越限：延时从零重新累计——清除前的恢复期不能计入新一轮越限。</summary>
        [Fact]
        public void 恢复_清除后再越限_需重新累计()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 255)));
            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(2), Value: 256)));
            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(2.5), Value: 245)));  // 深回起算
            Assert.Equal(FilterDecision.Clear, evaluator.Evaluate(new ConditionSample(At: T(3.5), Value: 244))); // 满 1s 清除

            var re1 = evaluator.Evaluate(new ConditionSample(At: T(10), Value: 261));   // 再越限：新一轮起点
            var re2 = evaluator.Evaluate(new ConditionSample(At: T(11.9), Value: 262)); // 新一轮仅 1.9s
            var re3 = evaluator.Evaluate(new ConditionSample(At: T(12), Value: 263));   // 满 2s 才升级

            Assert.Equal(FilterDecision.None, re1); // 恢复期(3.5→10s)不得计入
            Assert.Equal(FilterDecision.None, re2);
            Assert.Equal(FilterDecision.Raise, re3);
        }

        /// <summary>铁律二边界（哲学 §6.2「死区 ≥ 信号噪声幅值」）：零延时隔离死区——
        /// 噪声幅值(3℃)超过死区(2℃)时，值在两线外来回即翻转，死区保不了你：铁律不是白写的。
        /// 首条事实即越限且零延时：直接升级（#17 修正——零延时本就无「累计」可言，开机盲区不吞它）。</summary>
        [Fact]
        public void 死区_噪声幅值超过死区_照翻转()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.Zero, offDelay: TimeSpan.Zero);

            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 250.5)));   // 首条即越限：零延时立即升
            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(0.5), Value: 250.5))); // 报警中：不重播

            Assert.Equal(FilterDecision.Clear, evaluator.Evaluate(new ConditionSample(At: T(1), Value: 247.5)));   // 噪声低谷穿出恢复点
            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(1.5), Value: 250.5))); // 噪声高峰穿出限值
            Assert.Equal(FilterDecision.Clear, evaluator.Evaluate(new ConditionSample(At: T(2), Value: 247.5)));   // 照翻不误
        }

        /// <summary>
        /// 首条事实盲区修正（#17）：OnDelay = 0 的「即时触发」档案，开机时条件已越限——
        /// 首条事实直接升级，不等第二条定界（否则若第二条已回限内，这次越限永远不升，最坏漏报一段）。
        /// 延时为零没有「累计」可言：单条事实即够格。升级后报警中不重播照旧、深回清除照旧。
        /// </summary>
        [Fact]
        public void 盲区修正_首条事实_零延时且已越限_立即升()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.Zero, offDelay: TimeSpan.Zero);

            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 260))); // 开机即越限：立即升
            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(1), Value: 261)));  // 报警中：不重播
            Assert.Equal(FilterDecision.Clear, evaluator.Evaluate(new ConditionSample(At: T(2), Value: 245))); // 深回即清（恢复延时 0）
        }

        /// <summary>首条事实的既有契约不变（#17 只动零延时一格）：带触发延时的档案，首条立段不升——
        /// 没有前序事实就无从认定「持续」，延时未满照旧静默，满延时照常升级。</summary>
        [Fact]
        public void 首条事实_有延时_照旧立段静默()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.Zero);

            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 260))); // 首条立段：延时未满
            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(1.9), Value: 261)));
            Assert.Equal(FilterDecision.Raise, evaluator.Evaluate(new ConditionSample(At: T(2), Value: 262))); // 满延时照常
        }

        /// <summary>乱序事实（时间戳倒流）：忽略不计——不许倒拨累计，也不许把进行中的越限段当「回限内」清零。</summary>
        [Fact]
        public void 时间_乱序事实_忽略不计()
        {
            var evaluator = new HysteresisEvaluator(limit: 250, deadband: 2, onDelay: TimeSpan.FromSeconds(2), offDelay: TimeSpan.FromSeconds(1));

            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(0), Value: 260))); // 越限段起点
            Assert.Equal(FilterDecision.None, evaluator.Evaluate(new ConditionSample(At: T(1), Value: 261))); // 累计 1s
            var rewind = evaluator.Evaluate(new ConditionSample(At: T(0.5), Value: 200)); // 乱序 + 限内：不许清零进行中的段
            var confirm = evaluator.Evaluate(new ConditionSample(At: T(2), Value: 262));  // 1→2s 补满延时

            Assert.Equal(FilterDecision.None, rewind);
            Assert.Equal(FilterDecision.Raise, confirm); // 若乱序被当真，越限段已被清零，这里到不了升级
        }

        /// <summary>测试时基：从 2026-01-01 起的秒表。</summary>
        internal static DateTimeOffset T(double seconds) => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(seconds);
    }
}
