namespace UI.FrameWork.Core.Main
{
    /// <summary>
    /// 顶栏报警按钮的汇总级别。壳自己的中性模型：不认识报警模块，只有「按钮长什么样」。
    /// 家族 UI（Framework.Alarm.UI）负责把报警引擎的活跃快照翻译成这里的三件套（级别/计数/文本），
    /// 翻译规则见家族侧 AlarmHeaderStatus（词汇表「顶栏汇总状态」）。
    /// </summary>
    public enum HeaderAlarmLevel
    {
        /// <summary>无活账：按钮灰显，不闪。</summary>
        Normal,

        /// <summary>有未确认的低优先级报警：黄闪，责任还没接管。</summary>
        Warning,

        /// <summary>有未确认的中/高优先级报警：红闪，最要紧的一档。</summary>
        Alarm,

        /// <summary>报警还挂着但都已确认：稳红，责任已接管、尚未闭环。</summary>
        AlarmAcknowledged,

        /// <summary>都已恢复（RTN）等待确认收尾：橙稳，等一个「看见了」。</summary>
        Solved,
    }
}
