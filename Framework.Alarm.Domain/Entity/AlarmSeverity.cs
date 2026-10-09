namespace Framework.Alarm.Domain
{
    /// <summary>
    /// 后果严重度分档（MADB「严重度档」栏）——优先级矩阵的**输入**（哲学 §4.2；
    /// CONTEXT.md「严重度」：严重度是输入，优先级是输出，两者不要混用）。
    /// 分档含义是占位示例，宿主按自家工艺哲学重新定义。
    /// </summary>
    public enum AlarmSeverity
    {
        /// <summary>S1：人身安全 / 重大环境。</summary>
        S1,

        /// <summary>S2：停产 / 设备损坏。</summary>
        S2,

        /// <summary>S3：质量 / 产量 / 经济性。</summary>
        S3,
    }
}
