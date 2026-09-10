namespace Framework.Device
{
    /// <summary>
    /// 一帧相机图像，以托管字节数组 + 宽高 + 像素格式为规范表示，不泄漏裸指针。
    /// </summary>
    public class CameraData
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        public int Width { get; set; }

        public int Height { get; set; }

        public PixelFormat PixelFormat { get; set; }
    }
}
