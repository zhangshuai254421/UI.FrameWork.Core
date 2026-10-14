namespace Framework.Alarm
{
    /// <summary>
    /// 空条件源：一个永不报条件事实的 <see cref="IConditionSource"/>。
    /// 给「还没接采集层的宿主」兜底——引擎照样构造得起来，只是一声不吭；
    /// 宿主接了真采集层，把真源注册在 <see cref="AlarmServiceCollectionExtensions.AddAlarmEngine"/> 之前即生效（TryAdd 让位）。
    /// </summary>
    public sealed class EmptyConditionSource : IConditionSource
    {
        /// <summary>条件事实出口：空源没有事实，这个事件永远不响。</summary>
        public event Action<string, ConditionSample>? ConditionChanged;
    }
}
