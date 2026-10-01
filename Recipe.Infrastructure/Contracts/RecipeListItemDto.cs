namespace Recipe.Infrastructure.Contracts
{
    /// <summary>
    /// 配方列表项（ADR 0002）：查询投影结果，供列表页只读展示与选中定位使用。
    /// </summary>
    public class RecipeListItemDto
    {
        /// <summary>配方 Id（主键）。</summary>
        public int Id { get; set; }

        /// <summary>文件夹（分组）名。</summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>配方名。</summary>
        public string RecipeName { get; set; } = string.Empty;
    }
}
