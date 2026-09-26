using Prism.Events;

namespace Recipe.Infrastructure.Contracts
{
    /// <summary>
    /// 配方数据变更事件（ADR 0002 规则 4）：由 service 在写操作（复制/重命名/删除/应用）完成后发布，
    /// 页面订阅刷新，写入方无需自己通知。
    /// </summary>
    public class RecipeChangedEvent : PubSubEvent<RecipeChangedPayload>
    {
    }
}
