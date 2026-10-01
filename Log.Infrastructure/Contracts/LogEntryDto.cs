namespace Log.Infrastructure.Contracts
{
    /// <summary>
    /// 日志列表项（ADR 0002）：查询投影结果，供日志查看页只读展示。
    /// </summary>
    public class LogEntryDto
    {
        /// <summary>日志记录主键。</summary>
        public int Id { get; set; }

        /// <summary>日志时间戳字符串。</summary>
        public string? Timestamp { get; set; }

        /// <summary>日志级别。</summary>
        public string? Level { get; set; }

        /// <summary>渲染后的日志消息文本。</summary>
        public string? RenderedMessage { get; set; }
    }
}
