namespace Framework.Alarm
{
    /// <summary>
    /// 报警馈送接缝（摘要 §8.4）：引擎事件流的出口契约——落库（<see cref="EventHistory"/>）
    /// 与呈现层订阅都从这里走，谁在生产事件、谁在消费事件互不相认。
    /// <see cref="AlarmEngine"/> 是 v1 唯一实现。
    /// 有意收窄：摘要 §8.4 的接缝形状含确认/搁置等操作成员，本接口 v1 只收 <see cref="EventRaised"/>
    /// 一员——历史库只消费事件流，不产动作；操作成员随呈现层工单（v2）按需另立接口，不在这里预铺。
    /// </summary>
    public interface IAlarmFeed
    {
        /// <summary>事件流：引擎产出的每一拍（append-only 落库、推 UI 的挂钩，摘要 §8.11）。</summary>
        event Action<AlarmEvent>? EventRaised;
    }
}
