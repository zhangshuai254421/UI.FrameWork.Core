using System.Threading.Channels;

namespace Framework.Alarm
{
    /// <summary>
    /// 事件历史（摘要 §8.11）：订阅引擎的事件出口，把事件先放进内存队列，由后台唯一的写者线程
    /// 按进队顺序逐条写入 <see cref="IEventStore.Append"/>（#31）。产事件的线程只付出队成本
    /// （TryWrite 永不阻塞），写库的耗时都在后台。写入只增不改（append-only），包括带搁置期限的事件：
    /// 引擎记录全量事实，历史一条不缺。一条写完才取下一条，库里的顺序就是事件发生的顺序，
    /// 搁置登记的回放（#16）依赖这个顺序。
    /// 为什么要这个队列（#31）：以前 Append 直接挂在引擎的 EventRaised 上，而引擎全程持锁发事件，
    /// 库被外部独占（备份、杀毒软件，#29 场景）时 SQLite busy 等待会传回引擎关键区，冻住所有 UI 线程调用。
    /// 队列把写库挪出关键区：库再慢，卡住的只是后台写者，事实流和 UI 不受影响。
    /// 代价（ADR-0011 拍板）：进程在写完之前崩溃，队列里的尾部事件会丢。在「履历完整」和「不冻屏」
    /// 之间选了不冻屏；库长时间进不去时队列有上限（满了丢新保旧），先发生的事件优先存活。
    /// 写库失败（busy 超时、磁盘抖动）由后台按固定间隔重试到成功为止。重试是订阅方自己的职责，
    /// 一家订阅方失败不许拖垮事实流（引擎的 Raise 契约）；顺序不跳位，也不补写假事件。
    /// 写入路径只有存储的 Append，本类不提供任何修改历史的手段；引擎没发的事件库里也不会有。
    /// 生命周期随引擎：引擎重建换档案后，旧引擎 <see cref="AlarmEngine.Dispose"/> 退订采集层，
    /// 不再产事件，本类的写入随之自然停止，不会出现两份重复落库。<see cref="Dispose"/> 停止接收、
    /// 关闭通道，限时写完存量再返回（测试断言前先 Dispose 即代表全量落定；宿主关闭时不丢队列里的事件）。
    /// </summary>
    public sealed class EventHistory : IDisposable
    {
        /// <summary>事件队列：容量封顶（默认一万条），满了丢新保旧，先发生的事件优先存活（ADR-0011）。</summary>
        private readonly Channel<AlarmEvent> _queue;

        private readonly IEventStore _store;
        private readonly IAlarmFeed _feed;

        /// <summary>写库失败后的重试间隔：给外部锁（备份、杀毒）留解除的时间，也不许热旋空转。</summary>
        private readonly TimeSpan _retryDelay;

        /// <summary>后台唯一写者：按进队顺序逐条写库，<see cref="Dispose"/> 会等它写完存量。</summary>
        private readonly Task _drain;

        /// <summary>停止信号：重试循环看到它就放弃在途重试。库彻底坏了时不许拖住宿主关闭。</summary>
        private volatile bool _stopping;

        /// <summary>接线：订阅引擎事件即入队，后台单写者写库。</summary>
        /// <param name="store">事件历史库：每条事件的去处（写库失败的重试由后台写者承担）。</param>
        /// <param name="feed">引擎接缝：引擎产出的每条事件从这里进来。</param>
        /// <param name="capacity">队列容量：库长时间进不去时的存量上限，满了丢新保旧。</param>
        /// <param name="retryDelay">写库失败的重试间隔；测试传近零值就不用等真时钟。</param>
        public EventHistory(IEventStore store, IAlarmFeed feed, int capacity = 10_000, TimeSpan? retryDelay = null)
        {
            _store = store;
            _feed = feed;
            _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
            _queue = Channel.CreateBounded<AlarmEvent>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropWrite,   // 满了丢新保旧：先发生的证据优先存活
            });
            _feed.EventRaised += Enqueue;                      // 订阅即入队（一行接线，产事件的线程不等写库）
            _drain = Task.Run(DrainAsync);                     // 后台写者开工
        }

        /// <summary>接线：引擎产一条入一条。入队永不阻塞，满了按通道规则丢新（ADR-0011 的取舍）。</summary>
        private void Enqueue(AlarmEvent e) => _queue.Writer.TryWrite(e);

        /// <summary>写库循环：按进队顺序逐条写入，一条写完才取下一条，库里的顺序就是事件顺序；
        /// 队列清空且通道关闭后循环自然结束。第一次尝试无条件进行，停止信号只拦重试，不拦存量。</summary>
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
                            return; // 收到停止信号（<see cref="Dispose"/> 在等）：没写完的随进程去，不拖宿主关闭
                        }

                        // 库暂时进不去（busy 超时、磁盘抖动）：等一拍再试。事实流和 UI 不在这里，等得起
                        await Task.Delay(_retryDelay).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>关闭：停止接收 → 关闭通道 → 等存量写完。等待限时（5 个重试间隔）：库真被锁死
        /// （Append 卡在 busy 窗口里）时关闭照样能返回，没写完的随进程去（ADR-0011 崩溃窗口取舍
        /// 在关闭路径上的延伸）；库正常时毫秒级写完，限时用不上。</summary>
        public void Dispose()
        {
            _feed.EventRaised -= Enqueue;                      // 先停止接收：之后的事件不再进队列
            _queue.Writer.TryComplete();                       // 关闭通道：ReadAllAsync 写完存量后自然结束
            _stopping = true;                                  // 在途重试立即放弃
            _drain.Wait(_retryDelay * 5);                      // 限时等待（5 个重试间隔），不拖宿主关屏
        }
    }
}
