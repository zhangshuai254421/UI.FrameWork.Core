namespace Recipe.Infrastructure.Contracts
{
    /// <summary>
    /// 配方数据变更类型。
    /// </summary>
    public enum RecipeChangeKind
    {
        /// <summary>新增。</summary>
        Added,
        /// <summary>更新。</summary>
        Updated,
        /// <summary>删除。</summary>
        Removed,
    }

    /// <summary>
    /// 配方数据变更事件载荷（ADR 0002 规则 4）：service 在写操作完成后发布，
    /// 列表页按 Id 整条替换/增删，或整批刷新。
    /// </summary>
    public class RecipeChangedPayload
    {
        /// <summary>变更类型：新增、更新或删除。</summary>
        public RecipeChangeKind Kind { get; set; }

        /// <summary>变更涉及的配方 Id。</summary>
        public int RecipeId { get; set; }

        /// <summary>配方所在文件夹（分组）名。</summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>配方名。</summary>
        public string RecipeName { get; set; } = string.Empty;
    }
}
