using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Log.Domain
{
    /// <summary>
    /// 日志历史实体：对应 Serilog 落库表 SerilogHistory，一行记录一条日志。
    /// </summary>
    public partial class SerilogHistory:Entity
    {
        /// <summary>日志时间戳（Serilog 渲染的字符串格式，可按字典序比较）。</summary>
        public string? Timestamp { get; set; }

        /// <summary>日志级别（如 Information、Error），数据库中为定长字符串。</summary>
        [Column(TypeName = "VARCHAR(10)")]
        public string? Level { get; set; }

        /// <summary>异常详情（含堆栈），无异常时为 null。</summary>
        public string? Exception { get; set; }

        /// <summary>渲染后的日志消息文本。</summary>
        public string? RenderedMessage { get; set; }

        /// <summary>结构化属性（JSON 字符串）。</summary>
        public string? Properties { get; set; }
    }
}
