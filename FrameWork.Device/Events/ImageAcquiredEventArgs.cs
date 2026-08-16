namespace FrameWork.Device
{
    /// <summary>
    /// 图像采集完成事件参数
    /// </summary>
    public class ImageAcquiredEventArgs : EventArgs
    {
        /// <summary>相机设备编码</summary>
        public string DeviceCode { get; }

        /// <summary>图像宽度（像素）</summary>
        public int Width { get; }

        /// <summary>图像高度（像素）</summary>
        public int Height { get; }

        /// <summary>像素格式</summary>
        public string PixelFormat { get; }

        /// <summary>图像数据</summary>
        public byte[] ImageData { get; }

        /// <summary>采集时间戳</summary>
        public DateTime Timestamp { get; }

        /// <summary>图像数据长度（字节）</summary>
        public int DataLength => ImageData?.Length ?? 0;

        public ImageAcquiredEventArgs(
            string deviceCode,
            int width,
            int height,
            string pixelFormat,
            byte[] imageData)
        {
            DeviceCode = deviceCode;
            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
            ImageData = imageData;
            Timestamp = DateTime.Now;
        }
    }
}
