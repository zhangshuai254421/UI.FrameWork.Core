namespace Framework.Detection
{
    /// <summary>
    /// 一条检测结果：目标在原图像素坐标系下的外接矩形、类别与置信度。
    /// 坐标以左上角为原点，与 <see cref="Framework.Imaging.ImageFrame"/> 的像素坐标一致。
    /// </summary>
    /// <param name="X">外接矩形左上角 X（像素）。</param>
    /// <param name="Y">外接矩形左上角 Y（像素）。</param>
    /// <param name="Width">外接矩形宽（像素）。</param>
    /// <param name="Height">外接矩形高（像素）。</param>
    /// <param name="Label">类别名（来自模型标签）。</param>
    /// <param name="Confidence">置信度 0~1。</param>
    public sealed record Detection(double X, double Y, double Width, double Height, string Label, double Confidence);
}
