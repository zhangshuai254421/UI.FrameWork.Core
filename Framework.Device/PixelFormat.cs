namespace Framework.Device
{
    /// <summary>
    /// 品牌无关的像素格式（相机图像的规范像素表示）。
    /// </summary>
    public enum PixelFormat
    {
        Undefined = 0,
        Mono8 = 1,
        Mono10 = 2,
        Mono12 = 3,
        Mono16 = 4,
        BayerRG8 = 5,
        BayerBG8 = 6,
        BayerGR8 = 7,
        BayerGB8 = 8,
        RGB8 = 9,
        BGR8 = 10,
    }
}
