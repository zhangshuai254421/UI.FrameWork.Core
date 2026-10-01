namespace Framework.Imaging
{
    /// <summary>
    /// 图像帧：一帧图像数据的快照（像素数据 + 宽高 + 像素格式），由相机帧映射而来，与具体设备无关。
    /// 系统级图像原语：图像查看控件与检测能力都以它为输入。
    /// 实例创建后不可变。所有权契约：消费方把像素转换进自己的缓冲后即不再引用 <see cref="Data"/>，
    /// 泵方可以每帧新建数组，也可以转换完成后复用/归还池化数组。
    /// </summary>
    public sealed class ImageFrame
    {
        /// <summary>创建一帧图像。参数不合法（尺寸非正、数据不足）时抛 <see cref="ArgumentException"/>。</summary>
        public ImageFrame(int width, int height, ImagePixelFormat pixelFormat, byte[] data)
        {
            if (width <= 0)
            {
                throw new ArgumentException("宽度必须为正数。", nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentException("高度必须为正数。", nameof(height));
            }

            int required = width * height * ImagePixelConverter.GetBytesPerPixel(pixelFormat);
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length < required)
            {
                throw new ArgumentException(
                    $"像素数据不足：格式 {pixelFormat}、尺寸 {width}×{height} 至少需要 {required} 字节，实际 {data.Length} 字节。",
                    nameof(data));
            }

            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
            Data = data;
        }

        /// <summary>图像宽度（像素）。</summary>
        public int Width { get; }

        /// <summary>图像高度（像素）。</summary>
        public int Height { get; }

        /// <summary>像素格式。</summary>
        public ImagePixelFormat PixelFormat { get; }

        /// <summary>像素数据，紧凑排布（无行对齐填充），字节序见 <see cref="ImagePixelFormat"/>。</summary>
        public byte[] Data { get; }
    }
}
