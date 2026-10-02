namespace Framework.Alarm
{
    /// <summary>
    /// 主报警数据库（MADB）一行的代码投影——每条报警的出生档案（哲学 §6.1【强制】，摘要 §5.5）。
    /// 合理化评审的法定产出；引擎两条契约都以它为前提：无档案不报警、组态参数来自档案（档案驱动，
    /// 不在 <c>if</c> 里长报警）。位号全系统唯一；操作动作必填——填不出的候选当庭退回。
    /// </summary>
    /// <param name="Tag">位号：全系统唯一 ID（如 HEAT01_TEMP_HI）。</param>
    /// <param name="Description">描述：发生了什么，一句话含方向（如「加热板温度高于工艺上限」）。</param>
    /// <param name="Class">分类：安全 / 环境 / 设备 / 工艺。</param>
    /// <param name="Consequence">不处理后果：放着不管会怎样。</param>
    /// <param name="Cause">最可能原因：排查起点。</param>
    /// <param name="OperatorAction">操作动作：第三问的答案，第一步干什么（必填）。</param>
    /// <param name="MaxResponseTime">MTR：后果发生前的行动窗口；触发延时应 &lt; 它（铁律一）。</param>
    /// <param name="Severity">严重度档 S1/S2/S3：矩阵输入。</param>
    /// <param name="MtrTier">矩阵坐标 T 轴（响应时间分档）：与 <paramref name="Severity"/> 组成坐标 (S × T)。</param>
    /// <param name="Priority">优先级：查矩阵的输出，固定三级。</param>
    /// <param name="Limit">组态参数：限值（触发点，来自合理化，非出厂默认）。</param>
    /// <param name="Deadband">组态参数：死区（恢复点 = 限值 − 死区，≥ 信号噪声幅值，铁律二）。</param>
    /// <param name="OnDelay">组态参数：触发延时（略长于毛刺时长，且 &lt; <paramref name="MaxResponseTime"/>）。</param>
    /// <param name="OffDelay">组态参数：恢复延时（条件在限附近抖动时才需要，可为零）。</param>
    /// <param name="Group">组态参数：分组（区域/工段/模式），支撑画面过滤与停机态批量抑制。</param>
    /// <param name="RationalizedBy">评审留痕：评审人。</param>
    /// <param name="RationalizedOn">评审留痕：评审日期。</param>
    /// <param name="PhilosophyVersion">评审留痕：评审所依据的报警哲学版本（哲学改版，档案回炉的线索）。</param>
    public sealed record AlarmDefinition(
        string Tag,
        string Description,
        AlarmClass Class,
        string Consequence,
        string Cause,
        string OperatorAction,
        TimeSpan MaxResponseTime,
        AlarmSeverity Severity,
        ResponseTimeTier MtrTier,
        AlarmPriority Priority,
        double Limit,
        double Deadband,
        TimeSpan OnDelay,
        TimeSpan OffDelay,
        string Group,
        string RationalizedBy,
        DateTimeOffset RationalizedOn,
        string PhilosophyVersion)
    {
        /// <summary>
        /// 当庭退回：档案入 MADB 前的法定校验（哲学 §6.1「填不出的候选当庭退回」），存储增/改前调用。
        /// 操作动作必填；触发延时必须小于 MTR（铁律一，哲学 §6.2——响应窗口从越限起算，
        /// 延时 ≥ MTR 等于把行动窗口吃尽，报警时已无从响应）。
        /// 铁律二（死区 ≥ 噪声幅值）不在此校验：噪声幅值是信号属性，档案里没有，由合理化评审把关。
        /// </summary>
        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(OperatorAction))
            {
                throw new ArgumentException("操作动作必填：填不出的候选当庭退回（哲学 §6.1）", nameof(OperatorAction));
            }

            if (OnDelay >= MaxResponseTime)
            {
                throw new ArgumentException(
                    $"触发延时（{OnDelay}）必须小于 MTR（{MaxResponseTime}）：否则响应窗口被延时吞尽（铁律一，哲学 §6.2）",
                    nameof(OnDelay));
            }
        }
    }
}
