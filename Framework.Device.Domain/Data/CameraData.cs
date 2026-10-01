namespace Framework.Device.Domain
{
    /// <summary>
    /// 一帧相机图像，以托管字节数组 + 宽高 + 像素格式为规范表示，不泄漏裸指针。
    /// </summary>
    public class CameraData
    {
        /// <summary>图像数据：按行紧凑排列的原始像素（无行对齐填充），字节布局由 <see cref="PixelFormat"/> 决定。</summary>
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        /// <summary>图像宽度（像素）。</summary>
        public int Width { get; set; }

        /// <summary>图像高度（像素）。</summary>
        public int Height { get; set; }

        /// <summary>像素格式；适配器翻译不了的厂商格式为 <see cref="PixelFormat.Undefined"/>。</summary>
        public PixelFormat PixelFormat { get; set; }
    }
}
