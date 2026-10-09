namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 数据流走查六步剧本（摘要 §8.6，#12 引擎总装联测同款）：步 1 报值 → 步 2–3 满延时 →
    /// 步 4 Raised 发出 → 步 5 确认后降温闭环 → 步 6 没人确认自行恢复。两个分支一条流跑完，
    /// 期望事件五条、先后不乱。引擎总装测试（AlarmEngineTests.集成_数据流走查六拍_两分支）
    /// 因为每步内嵌断言（步 4 事件发出、列表清空等）不适合抽出，保持原样。
    /// </summary>
    internal static class SixBeatScript
    {
        /// <summary>剧本全程的期望事件序列（事件类型名，按发生先后）。</summary>
        public static readonly string[] ExpectedEventTypes =
        {
            nameof(AlarmRaised), nameof(AlarmAcknowledged), nameof(AlarmClosed),
            nameof(AlarmRaised), nameof(AlarmAutoReturned),
        };

        /// <summary>跑一遍剧本：喂条件 + 拨时钟 + 确认，事件经引擎 EventRaised 发出。</summary>
        public static void Run(FakeConditionSource source, AlarmEngine engine, FakeClock clock)
        {
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));    // 步 1：报值（越限）
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));    // 步 2–3：查档案、满延时 → Raised
            clock.Advance(TimeSpan.FromSeconds(3));                                                                      // 三秒后操作员看见
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "李四");                                                      // 步 5 前半：确认 → Acknowledged（时钟当前值）
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2.5), Value: 245));  // 步 5 后半：降温过恢复点 → Closed
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 270));   // 步 6 前半：再越限，新一轮延时
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 271));   // 满 2s → 又 Raised
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12.5), Value: 240)); // 步 6 后半：没人确认自行恢复 → AutoReturned
        }
    }
}
