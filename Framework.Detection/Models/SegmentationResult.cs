namespace Framework.Detection
{
    /// <summary>
    /// 一次分割找到的一个实例：外接矩形、类别、置信度，外加逐像素掩码。
    /// 矩形在图像像素坐标系下（与 <see cref="Detection"/> 同规，保证界内）；
    /// 掩码与矩形同尺寸同原点，掩码像素 (mx, my) 对应图像像素 (X+mx, Y+my)。
    /// </summary>
    public sealed record SegmentationResult(
        double X,
        double Y,
        double Width,
        double Height,
        string Label,
        double Confidence,
        SegmentMask Mask);
}
