namespace Recipe.Infrastructure.Contracts
{
    /// <summary>
    /// 配方列表项（ADR 0002）：查询投影结果，供列表页只读展示与选中定位使用。
    /// </summary>
    public class RecipeListItemDto
    {
        public int Id { get; set; }

        public string GroupName { get; set; } = string.Empty;

        public string RecipeName { get; set; } = string.Empty;
    }
}
