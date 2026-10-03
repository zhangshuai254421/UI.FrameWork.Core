namespace Framework.Alarm.Tests
{
    /// <summary>内存事件存储过契约——与 SQLite 实现同一套断言。</summary>
    public class InMemoryEventStoreTests
    {
        [Fact]
        public void 追加_查询可见_原样读回() => EventStoreContract.追加_查询可见_原样读回(new InMemoryEventStore());

        [Fact]
        public void 四种事件_往返保真() => EventStoreContract.四种事件_往返保真(new InMemoryEventStore());

        [Fact]
        public void 搁置事件与带标升级_往返保真() => EventStoreContract.搁置事件与带标升级_往返保真(new InMemoryEventStore());

        [Fact]
        public void 查询_窗口外不可见() => EventStoreContract.查询_窗口外不可见(new InMemoryEventStore());

        [Fact]
        public void 查询_按位号过滤() => EventStoreContract.查询_按位号过滤(new InMemoryEventStore());

        [Fact]
        public void 查询_按落库先后返回() => EventStoreContract.查询_按落库先后返回(new InMemoryEventStore());

        [Fact]
        public void 只进不改_重复追加_两条都留() => EventStoreContract.只进不改_重复追加_两条都留(new InMemoryEventStore());

        [Fact]
        public void 接口_只有Append与Query_无改写路径() => EventStoreContract.接口_只有Append与Query_无改写路径();
    }
}
