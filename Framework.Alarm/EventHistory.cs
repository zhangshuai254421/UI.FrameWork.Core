namespace Framework.Alarm
{
    /// <summary>
    /// 事件历史（摘要 §8.11）：订阅即落库，一行接线——构造时把馈送的事件流接到
    /// <see cref="IEventStore.Append"/>，引擎产出的每一拍自动进本子（append-only，
    /// 包括带搁置标的事件，标本身随搁置工单落地）。写入路径只有存储的 Append，
    /// 本类不提供任何改写手段；引擎静默的拍子库里也无假事件。
    /// 生命周期随引擎：引擎重建换档案时，旧引擎 <see cref="AlarmEngine.Dispose"/>
    /// 退订采集层后不再产事件，本接线随之自然哑火，不留双份落库。
    /// </summary>
    public sealed class EventHistory
    {
        /// <summary>接线：订阅即落库。</summary>
        public EventHistory(IEventStore store, IAlarmFeed feed) => feed.EventRaised += store.Append;
    }
}
