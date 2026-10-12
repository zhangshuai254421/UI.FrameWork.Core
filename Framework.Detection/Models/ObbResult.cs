namespace Framework.Detection
{
    /// <summary>
    /// 一次旋转框检测找到的一个目标：未旋转矩形、旋转角、类别、置信度。
    /// <para>X/Y/Width/Height 是未旋转的外接矩形（图像像素坐标系，保证界内）；
    /// 目标本体 = 该矩形绕自身中心 (X+W/2, Y+H/2) 旋转 <see cref="AngleRadians"/> 弧度后的四边形
    /// （与推理引擎绘制约定一致）。旋转后角点可能超出图像范围，不裁剪——
    /// 裁剪会破坏四边形形状，目标部分出画幅本身是有效信息。</para>
    /// </summary>
    public sealed record ObbResult(
        double X,
        double Y,
        double Width,
        double Height,
        double AngleRadians,
        string Label,
        double Confidence);
}
