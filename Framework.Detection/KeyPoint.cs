namespace Framework.Detection
{
    /// <summary>
    /// 一个关键点：图像像素坐标下的位置与该点的置信度。
    /// 点在数组中的顺序即模型定义的关键点顺序（如 COCO 17 点的拓扑）。
    /// 允许坐标落在图像外（遮挡或延伸出画面的部位），显示侧自行裁剪。
    /// </summary>
    /// <param name="X">X（像素）。</param>
    /// <param name="Y">Y（像素）。</param>
    /// <param name="Confidence">该点置信度 0~1。</param>
    public sealed record KeyPoint(double X, double Y, double Confidence);
}
