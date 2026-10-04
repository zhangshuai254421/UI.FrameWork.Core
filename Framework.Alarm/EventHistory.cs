using System.Threading.Channels;

namespace Framework.Alarm
{
    /// <summary>
    /// 事件历史（摘要 §8.11）：订阅即入队，后台单写者冲账（#31）——构造时把馈送的事件流接进
    /// 内存队列（引擎产事件的线程只付入队成本，TryWrite 永不阻塞），后台唯一写者按进队次序
    /// 逐条 <see cref="IEventStore.Append"/>（append-only，包括带搁置标的事件——引擎不撒谎，
    /// 历史全量一拍不缺）。一条落定才取下一条：本子次序即履历次序，搁置单回放（#16）靠它。
    /// 为什么改（#31）：Append 曾直订在引擎 EventRaised 上，而引擎全程持 _gate 播报——库被外部
    /// 独占（备份/杀毒，#29 场景）时 SQLite busy 等待穿透回引擎关键区，冻住所有 UI 线程调用；
    /// 写后队列把落库挪出关键区：库再慢，冻的是后台写者，不是事实流与 UI。
    /// 代价（ADR-0011 拍板）：进程在冲账前崩溃，队列里的尾部事件丢失——履历完整性与冻屏之间
    /// 选了不冻屏；库长时间进不去时队列封顶（满了丢新保旧），最先发生的证据优先存活。
    /// 落库失败（busy 超时、磁盘抖动）在后台按拍重试到成功为止——重试是订阅方自己的职责，
    /// 一家落库失败不许拖垮事实流（引擎 Raise 契约）；顺序不跳位，绝不留半句假账。
    /// 写入路径只有存储的 Append，本类不提供任何改写手段；引擎静默的拍子库里也无假事件。
    /// 生命周期随引擎：引擎重建换档案时，旧引擎 <see cref="AlarmEngine.Dispose"/> 退订采集层后
    /// 不再产事件，本接线随之自然哑火，不留双份落库；<see cref="Dispose"/> 停进水、关闸、
    /// 冲完存量再收工（测试断言前冲账即全量落定；宿主收口时本子不欠账）。
    /// </summary>
    public sealed class EventHistory : IDisposable
    {
        /// <summary>进队通道：容量封顶（默认一万拍），满了丢新保旧——最先发生的证据优先存活（ADR-0011）。</summary>
        private readonly Channel<AlarmEvent> _queue;

        private readonly IEventStore _store;
        private readonly IAlarmFeed _feed;

        /// <summary>落库失败的重试间隔：给外部锁（备份/杀毒）留解除的空当，也不许热旋空转。</summary>
        private readonly TimeSpan _retryDelay;

        /// <summary>后台唯一写者：按进队次序逐条冲账，<see cref="Dispose"/> 等它清完存量。</summary>
        private readonly Task _drain;

        /// <summary>收工信号：重试循环看到它就放弃在途重试——库彻底坏了时不许拖住宿主收口。</summary>
        private volatile bool _stopping;

        /// <summary>接线：订阅即入队，后台单写者冲账。</summary>
        /// <param name="store">事件历史库：每一拍的去处（冲账后台承担落库失败的重试）。</param>
        /// <param name="feed">馈送接缝：引擎产出的每一拍。</param>
        /// <param name="capacity">队列容量：库长时间进不去时的存量上限，满了丢新保旧。</param>
        /// <param name="retryDelay">落库失败的重试间隔；测试传近零值即不等真钟。</param>
        public EventHistory(IEventStore store, IAlarmFeed feed, int capacity = 10_000, TimeSpan? retryDelay = null)
        {
            _store = store;
            _feed = feed;
            _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
            _queue = Channel.CreateBounded<AlarmEvent>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropWrite,   // 满了丢新保旧：履历头上不缺页
            });
            _feed.EventRaised += Enqueue;                      // 订阅即入队（一行接线，引擎线程不等落库）
            _drain = Task.Run(DrainAsync);                     // 后台单写者开工
        }

        /// <summary>接线：引擎产一拍入一拍——入队永不阻塞，满了按通道规矩丢新（ADR-0011 的取舍）。</summary>
        private void Enqueue(AlarmEvent e) => _queue.Writer.TryWrite(e);

        /// <summary>冲账循环：按进队次序逐条落库，一条落定才取下一条——本子次序即履历次序；
        /// 队列清空且关闸后自然收束。首试无条件——收工信号只拦重试，不拦账本上的存量。</summary>
        private async Task DrainAsync()
        {
            await foreach (var e in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                while (true)
                {
                    try
                    {
                        _store.Append(e);
                        break;
                    }
                    catch
                    {
                        if (_stopping)
                        {
                            return; // 收工让位（<see cref="Dispose"/>）：没冲完的账随进程去，不拖宿主不下班
                        }

                        // 库暂时进不去（busy 超时/磁盘抖动）：等一拍再试——事实流与 UI 不在这里，等得起
                        await Task.Delay(_retryDelay).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>收工冲账：停进水 → 关闸 → 等存量清完。库彻底坏了时在途重试让位收工，
        /// 没冲完的账随进程去（ADR-0011 的崩溃窗口取舍在收工口的延伸）。</summary>
        public void Dispose()
        {
            _feed.EventRaised -= Enqueue;                      // 先停进水：收工后的拍子不再欠账
            _queue.Writer.TryComplete();                       // 关闸：ReadAllAsync 清完存量自然收束
            _stopping = true;                                  // 在途重试别恋战
            _drain.Wait();                                     // 冲完才下班
        }
    }
}
