namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 数据流走查六拍剧本（摘要 §8.6，#12 引擎总装联测同款）：拍 1 报值 → 拍 2–3 满延时 →
    /// 拍 4 Raised 出流 → 拍 5 确认后降温闭环 → 拍 6 无人确认自愈。两分支一条流跑完，
    /// 期望履历五条、先后不乱。引擎总装测试（AlarmEngineTests.集成_数据流走查六拍_两分支）
    /// 因逐拍内嵌断言（拍 4 出流、列表清空等）不合抽出，留原样。
    /// </summary>
    internal static class SixBeatScript
    {
        /// <summary>剧本全程的期望履历（事件类型名，按落库先后）。</summary>
        public static readonly string[] ExpectedEventTypes =
        {
            nameof(AlarmRaised), nameof(AlarmAcknowledged), nameof(AlarmClosed),
            nameof(AlarmRaised), nameof(AlarmAutoReturned),
        };

        /// <summary>跑一遍剧本：喂条件 + 拨表 + 确认，事件经引擎 EventRaised 出流。</summary>
        public static void Run(FakeConditionSource source, AlarmEngine engine, FakeClock clock)
        {
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));    // 拍 1：报值（越限）
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266));    // 拍 2–3：查档案、满延时 → Raised
            clock.Advance(TimeSpan.FromSeconds(3));                                                                      // 三秒后操作员看见
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "李四");                                                      // 拍 5 前半：确认 → Acknowledged（时钟当下）
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2.5), Value: 245));  // 拍 5 后半：降温过恢复点 → Closed
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(10), Value: 270));   // 拍 6 前半：再越限，新一轮延时
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12), Value: 271));   // 满 2s → 又 Raised
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(12.5), Value: 240)); // 拍 6 后半：无人确认自愈 → AutoReturned
        }
    }
}
