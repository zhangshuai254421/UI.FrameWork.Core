using System.Collections.Generic;

namespace Framework.Detection
{
    /// <summary>
    /// 一条姿态估计结果：一个人体的外接矩形与关键点数组。
    /// 外接矩形保证位于图像范围内（与 <see cref="Detection"/> 同规则）；关键点坐标允许出界。
    /// </summary>
    /// <param name="X">外接矩形左上角 X（像素）。</param>
    /// <param name="Y">外接矩形左上角 Y（像素）。</param>
    /// <param name="Width">外接矩形宽（像素）。</param>
    /// <param name="Height">外接矩形高（像素）。</param>
    /// <param name="Label">类别名（姿态模型通常为 person）。</param>
    /// <param name="Confidence">整人置信度 0~1。</param>
    /// <param name="KeyPoints">关键点数组，顺序即模型拓扑顺序。</param>
    public sealed record PoseResult(
        double X, double Y, double Width, double Height,
        string Label, double Confidence,
        IReadOnlyList<KeyPoint> KeyPoints);
}
