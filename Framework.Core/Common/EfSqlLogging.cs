namespace Framework.Core.Common
{
    /// <summary>
    /// EF SQL 日志总开关（#30）：DEBUG 构建下默认安静——四家族（Alarm/Recipe/Log/Device）的
    /// DataContext.OnConfiguring 同吃这一个开关，别只修报警。背景：每操作全量 SQL 格式化
    /// （连参数值）在 UI 线程实打实做分配，报警演示页每秒一拍全走这条；操作员名/位号等
    /// 敏感值还泄进任何挂着的控制台/CI 捕获。
    /// 需要诊断时设环境变量 <c>EF_SQL_LOG=1</c>（或 true）显式打开——诊断利器留开关不删能力，
    /// 「出厂默认裸奔」的反而面是无人看的输出也在烧。
    /// </summary>
    public static class EfSqlLogging
    {
        /// <summary>诊断开关是否打开：环境变量 EF_SQL_LOG 取 1 或 true（大小写不敏感）。</summary>
        public static bool Enabled
            => Environment.GetEnvironmentVariable("EF_SQL_LOG") is { Length: > 0 } value
                && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }
}
