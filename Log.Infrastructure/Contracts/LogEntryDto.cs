namespace Log.Infrastructure.Contracts
{
    /// <summary>
    /// 日志列表项（ADR 0002）：查询投影结果，供日志查看页只读展示。
    /// </summary>
    public class LogEntryDto
    {
        public int Id { get; set; }

        public string? Timestamp { get; set; }

        public string? Level { get; set; }

        public string? RenderedMessage { get; set; }
    }
}
