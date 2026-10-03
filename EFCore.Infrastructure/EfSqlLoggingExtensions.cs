using Framework.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EFCore.Infrastructure
{
    /// <summary>
    /// EF SQL 诊断日志扩展（#30）：把 EfSqlLogging 开关的落地动作收成一句话，
    /// 四家族（Alarm/Recipe/Log/Device）DataContext.OnConfiguring 同吃——开关判定与
    /// 日志配置只有这一份，各家不会漂移（改级别/漏 EnableDetailedErrors 之类）。
    /// </summary>
    public static class EfSqlLoggingExtensions
    {
        /// <summary>开关打开（EF_SQL_LOG=1|true）时输出 SQL 日志、敏感数据日志与详细错误到控制台；
        /// 关着就什么都不做——DEBUG 默认安静，诊断能力留开关不删。</summary>
        public static DbContextOptionsBuilder UseDiagnosticSqlLogging(this DbContextOptionsBuilder builder)
        {
            if (EfSqlLogging.Enabled)
            {
                builder.LogTo(Console.WriteLine, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }

            return builder;
        }
    }
}
