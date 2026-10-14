namespace Framework.Alarm.Tests
{
    /// <summary>采集层替身：测试直接喂条件序列，引擎不直连设备（摘要 §8.4 的接口）。</summary>
    internal sealed class FakeConditionSource : IConditionSource
    {
        public event Action<string, ConditionSample>? ConditionChanged;

        /// <summary>模拟采集层报事实。</summary>
        public void Feed(string tag, ConditionSample sample) => ConditionChanged?.Invoke(tag, sample);
    }
}
