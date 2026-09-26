namespace Recipe.Infrastructure.Contracts
{
    /// <summary>
    /// 配方数据变更类型。
    /// </summary>
    public enum RecipeChangeKind
    {
        Added,
        Updated,
        Removed,
    }

    /// <summary>
    /// 配方数据变更事件载荷（ADR 0002 规则 4）：service 在写操作完成后发布，
    /// 列表页按 Id 整条替换/增删，或整批刷新。
    /// </summary>
    public class RecipeChangedPayload
    {
        public RecipeChangeKind Kind { get; set; }

        public int RecipeId { get; set; }

        public string GroupName { get; set; } = string.Empty;

        public string RecipeName { get; set; } = string.Empty;
    }
}
