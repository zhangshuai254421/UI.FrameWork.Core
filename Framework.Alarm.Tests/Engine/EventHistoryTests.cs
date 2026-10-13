namespace Framework.Alarm.Tests
{
    /// <summary>
    /// EventHistory 接线契约（摘要 §8.11、哲学 §8.1【强制】、#31 写后队列）：订阅即入队，
    /// 后台单写者落库——引擎产事件的线程只付出队成本，库再慢冻住的是后台写者，不是事实流与 UI；
    /// 每条事件最终进库（Dispose 写完存量后断言）、先后不乱；引擎静默的场合库里也没有假事件
    /// （不发事件也是正式输出）。
    /// 联测走真实引擎：FakeConditionSource 喂条件、替身存储收流，复用 #12 引擎总装的数据流走查六步剧本。
    /// </summary>
    public class EventHistoryTests
    {
        /// <summary>订阅即落库：引擎产一条事件，写完存量后库里多一行——一行接线，不需要谁记得手动落库。</summary>
        [Fact]
        public void 接线_引擎产事件_自动落库()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var store = new InMemoryEventStore();
            var history = new EventHistory(store, engine);

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // 满延时 → Raised
            history.Dispose();                              // 写完存量：后台写者清完队列再断言（#31 落库改异步）

            var raised = Assert.IsType<AlarmRaised>(Assert.Single(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10))));
            Assert.Equal(AlarmPriority.High, raised.Priority);              // 档案优先级随事件进库
            Assert.Equal(HysteresisEvaluatorTests.T(2), raised.At);
        }

        /// <summary>
        /// 验收联测：数据流走查六步剧本（摘要 §8.6，与引擎总装票同一剧本）跑完并写完存量，
        /// 库里恰好五条、先后不乱、内容无损——两个分支（确认闭环/没人看见自行恢复）的每条事件都有记录。
        /// </summary>
        [Fact]
        public void 接线_六步剧本_每一步在库()
        {
            var source = new FakeConditionSource();
            var clock = new FakeClock(HysteresisEvaluatorTests.T(0));
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), clock);
            var store = new InMemoryEventStore();
            var history = new EventHistory(store, engine);

            SixStepScript.Run(source, engine, clock);
            history.Dispose();                              // 写完存量：库的顺序就是事件顺序，写完再核对

            var history6 = store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(13));
            Assert.Equal(SixStepScript.ExpectedEventTypes, history6.Select(e => e.GetType().Name)); // 每条在库：记录完整，两个分支分开

            var first = Assert.IsType<AlarmRaised>(history6[0]);
            Assert.Equal(HysteresisEvaluatorTests.T(2), first.At);       // 条件驱动：带事实时间戳
            var closed = Assert.IsType<AlarmClosed>(history6[2]);
            Assert.Equal(HysteresisEvaluatorTests.T(2.5), closed.At);
            var last = Assert.IsType<AlarmAutoReturned>(history6[4]);
            Assert.Equal(HysteresisEvaluatorTests.T(12.5), last.At);     // 没人看见的恢复：单独留痕，绝没有混成 Closed
        }

        /// <summary>引擎静默，库里也干净（不发事件也是正式输出，§11 警示 19）：无档案位号、Normal 时确认、
        /// 重复确认——历史库里一条假事件都没有；KPI 统计不能被非法时序的假事件污染。</summary>
        [Fact]
        public void 接线_静默步_库里无假事件()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var store = new InMemoryEventStore();
            var history = new EventHistory(store, engine);

            source.Feed("NO_MADB_ENTRY", new ConditionSample(At: HysteresisEvaluatorTests.T(1), Value: 999)); // 无档案：静默
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "李四");          // Normal 时确认：静默

            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265));
            source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // Raised
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "李四");          // 第一次确认：真转换
            engine.Acknowledge(AlarmEngineTests.HEAT01, op: "王五");          // 重复确认：静默
            history.Dispose();                              // 写完存量：静默的场合不许有人替它补记录

            Assert.Equal(
                new[] { nameof(AlarmRaised), nameof(AlarmAcknowledged) },
                store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)).Select(e => e.GetType().Name));
        }

        /// <summary>库被锁住，事件流不等落库（#31 核心回归）：Append 卡住（库文件被外部独占），
        /// 引擎产事件的线程照常走完——落库在后台写者手里，busy 等待不再传回引擎关键区冻 UI；
        /// 解锁写完存量后那条事件仍在库里，一条不缺。</summary>
        [Fact]
        public async Task 库被锁住_事件流不等落库_解锁后写进历史库()
        {
            var source = new FakeConditionSource();
            var engine = new AlarmEngine(source, AlarmEngineTests.StoreWithSample(), new FakeClock(HysteresisEvaluatorTests.T(0)));
            var store = new BlockedEventStore();
            var history = new EventHistory(store, engine);

            var emit = Task.Run(() =>
            {
                source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(0), Value: 265)); // 越限段起点（静默）
                source.Feed(AlarmEngineTests.HEAT01, new ConditionSample(At: HysteresisEvaluatorTests.T(2), Value: 266)); // 满延时 → Raised
            });
            try
            {
                await emit.WaitAsync(TimeSpan.FromSeconds(2));           // 旧接线（同步落库）在这里冻到超时
            }
            catch (TimeoutException)
            {
                Assert.Fail("库被锁住时引擎产事件被落库拖住：写后队列未生效");
            }

            Assert.True(store.Entered.Wait(TimeSpan.FromSeconds(2)));    // 后台写者确实已进 Append（卡住）
            Assert.Empty(store.Landed);                                  // 未放行：还没写进库

            store.Gate.Set();                                            // 放行（外部锁解除）
            history.Dispose();                                           // 写完存量再收工

            var raised = Assert.IsType<AlarmRaised>(Assert.Single(store.Landed));
            Assert.Equal(HysteresisEvaluatorTests.T(2), raised.At);
        }

        /// <summary>队列封顶，丢新保旧（ADR-0011 的取舍）：容量 1 的队列，写者拿着第一条卡住，
        /// 后续两条只有一条能进队——满了再来的新事件出局，队里已在的旧事件活着；放行写完后
        /// 库里头两条都在、次序不乱、绝没有第三条——库长时间进不去时，先发生的事件优先存活。</summary>
        [Fact]
        public void 队列满_丢新保旧_历史库不缺号()
        {
            var store = new BlockedEventStore();
            var feed = new FakeFeed();
            var history = new EventHistory(store, feed, capacity: 1);

            feed.Emit(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(1), AlarmPriority.High)); // 写者取走，卡住
            Assert.True(store.Entered.Wait(TimeSpan.FromSeconds(2)));    // 写者确实拿着第一条
            feed.Emit(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High)); // 入队（最后一个空位）
            feed.Emit(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(3), AlarmPriority.High)); // 队列满：新事件出局

            store.Gate.Set();                                            // 放行（外部锁解除）
            history.Dispose();                                           // 写完存量

            Assert.Equal(
                [HysteresisEvaluatorTests.T(1), HysteresisEvaluatorTests.T(2)],
                store.Landed.Select(e => e.At));                         // 头两条都在、次序不乱
        }

        /// <summary>落库失败按间隔重试到成功（#31）：前两次失败（busy 超时的替身），后台写者等一个间隔再试——
        /// 等到写成功再收工：那条事件落库一次且只一次，失败的尝试不留假记录；顺序不跳位。</summary>
        [Fact]
        public void 落库失败_重试到成功_不重复不丢事件()
        {
            var store = new FlakyEventStore(failures: 2);
            var feed = new FakeFeed();
            var history = new EventHistory(store, feed, retryDelay: TimeSpan.FromMilliseconds(1)); // 测试不等真时钟

            feed.Emit(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High));
            Assert.True(store.LandedOnce.Wait(TimeSpan.FromSeconds(2)), "重试没等到成功"); // 等落库成功，不猜时间
            history.Dispose();                                           // 写完存量再收工

            var landed = Assert.Single(store.Landed);                    // 落库一次：不重复
            Assert.Equal(HysteresisEvaluatorTests.T(2), landed.At);
            Assert.Equal(3, store.Attempts);                             // 两次失败一次成功：重试真发生过
        }

        /// <summary>收工让位（#31）：库持续进不去（每次都失败的替身）时关闭——在途重试放弃、
        /// 没写完的随进程去，Dispose 收得回来，不许拖着宿主关屏等 busy 窗口。</summary>
        [Fact]
        public void 收工让位_库持续进不去_不拖收工()
        {
            var store = new FlakyEventStore(int.MaxValue);               // 每次都失败：库彻底坏掉
            var feed = new FakeFeed();
            var history = new EventHistory(store, feed, retryDelay: TimeSpan.FromMilliseconds(50));

            feed.Emit(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High));

            var dispose = Task.Run(history.Dispose);                     // 库治不好的话，关闭不许赖着不走
            Assert.True(dispose.Wait(TimeSpan.FromSeconds(2)), "库持续进不去时 Dispose 被重试拖住：收工让位未生效");
            Assert.Empty(store.Landed);                                  // 没写成：随进程去（ADR-0011 取舍在关闭路径上）
        }

        /// <summary>关闭限时（#31 评审）：库锁死在 Append 里（永不放行的替身，真库即 30 秒 busy 窗口）
        /// 时关闭照样能返回——等待限时 5 个重试间隔，没写完的随进程去，不许把冻结搬到关屏路上。</summary>
        [Fact]
        public void 收工限时_库锁死在Append里_收得回来()
        {
            var store = new BlockedEventStore();                         // 永不放行：Append 卡死在 busy 窗口
            var feed = new FakeFeed();
            var history = new EventHistory(store, feed, retryDelay: TimeSpan.FromMilliseconds(10)); // 限时 = 5 × 10ms = 50ms

            feed.Emit(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High));
            Assert.True(store.Entered.Wait(TimeSpan.FromSeconds(2)));    // 写者确实卡在 Append 里

            var dispose = Task.Run(history.Dispose);                     // 锁死也不许拖关闭
            Assert.True(dispose.Wait(TimeSpan.FromSeconds(2)), "库锁死时 Dispose 被在途 Append 拖住：限时收口未生效");
            Assert.Empty(store.Landed);                                  // 没写完：随进程去
        }

        /// <summary>关闭后不再接收：<see cref="EventHistory.Dispose"/> 退订馈送——关闭之后的事件
        /// 不再进队列（宿主关闭后引擎万一还有尾事件，也不写入）。</summary>
        [Fact]
        public void 收工后_不再接收事件()
        {
            var store = new InMemoryEventStore();
            var feed = new FakeFeed();
            var history = new EventHistory(store, feed);

            history.Dispose();
            feed.Emit(new AlarmRaised(AlarmEngineTests.HEAT01, HysteresisEvaluatorTests.T(2), AlarmPriority.High)); // 关闭之后的事件

            Assert.Empty(store.Query(HysteresisEvaluatorTests.T(0), HysteresisEvaluatorTests.T(10)));
        }
    }
}
