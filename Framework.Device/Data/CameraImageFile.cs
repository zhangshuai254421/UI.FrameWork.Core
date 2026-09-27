using System.Buffers.Binary;

namespace Framework.Device
{
    /// <summary>
    /// 把 <see cref="CameraData"/>（品牌无关的规范像素表示）编码为图片文件并落盘。
    /// 仅依赖 BCL 手写 BMP 编码，不引入图形库依赖；假定 <see cref="CameraData.ImageData"/>
    /// 按行紧凑排列（无行对齐填充）。空帧、格式不支持、数据不足或写盘失败时返回 false。
    /// </summary>
    public static class CameraImageFile
    {
        /// <summary>保存一帧图像到指定路径（默认 BMP）；目标目录不存在自动创建，同名文件覆盖，失败返回 false。</summary>
        public static bool Save(CameraData? image, string filePath, ImageFileFormat format = ImageFileFormat.Bmp)
        {
            try
            {
                byte[]? encoded = format switch
                {
                    ImageFileFormat.Bmp => EncodeBmp(image),
                    _ => null, // 预留：后续图片格式在此扩展
                };
                if (encoded is null)
                {
                    return false;
                }

                // 未带扩展名时补默认扩展名（当前仅支持 BMP）；调用方需保证已有扩展名与格式一致。
                if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
                {
                    filePath += ".bmp";
                }

                var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(filePath, encoded);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // 与设备方法契约一致：失败不抛异常，统一返回 false。
                return false;
            }
        }

        /// <summary>
        /// 编码为 BMP 字节流（8 位灰度带调色板 / 24 位真彩）。
        /// 返回 null 表示无法编码：空帧、像素格式不支持（含 Undefined）或数据长度不足。
        /// </summary>
        private static byte[]? EncodeBmp(CameraData? image)
        {
            if (image is null || image.Width <= 0 || image.Height <= 0 || image.ImageData is null)
            {
                return null;
            }

            int width = image.Width;
            int height = image.Height;
            ReadOnlySpan<byte> pixels = image.ImageData;

            return image.PixelFormat switch
            {
                PixelFormat.Mono8 when pixels.Length >= width * height =>
                    EncodeGray8(pixels, width, height),

                // Mono10/12/16 同为 16 位小端容器、数据左对齐（高位有效），统一取高 8 位
                PixelFormat.Mono10 or PixelFormat.Mono12 or PixelFormat.Mono16
                    when pixels.Length >= width * height * 2 =>
                    EncodeGray16(pixels, width, height),

                // 拜耳阵列做双线性去马赛克还原为真彩
                PixelFormat.BayerRG8 or PixelFormat.BayerGR8 or PixelFormat.BayerGB8 or PixelFormat.BayerBG8
                    when pixels.Length >= width * height =>
                    EncodeBayer(pixels, width, height, image.PixelFormat),

                PixelFormat.RGB8 when pixels.Length >= width * height * 3 =>
                    EncodeRgb24(pixels, width, height, swapRAndB: true),
                PixelFormat.BGR8 when pixels.Length >= width * height * 3 =>
                    EncodeRgb24(pixels, width, height, swapRAndB: false),

                // Undefined 或数据长度不足：不猜测字节布局
                _ => null,
            };
        }

        /// <summary>创建 BMP 文件头（14 字节文件头 + 40 字节 BITMAPINFOHEADER，BI_RGB 不压缩，行序自下而上）。</summary>
        private static byte[] CreateBmpHeader(int width, int height, int bitsPerPixel, int paletteColors, out int pixelOffset, out int stride)
        {
            int paletteSize = paletteColors * 4;
            stride = (width * bitsPerPixel + 31) / 32 * 4; // 行按 4 字节对齐
            int fileSize = 14 + 40 + paletteSize + stride * height;
            var bmp = new byte[fileSize];

            bmp[0] = (byte)'B';
            bmp[1] = (byte)'M';
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(2), (uint)fileSize);
            pixelOffset = 14 + 40 + paletteSize;
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(10), (uint)pixelOffset);

            BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
            BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), width);
            BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), height); // 正值 = 自下而上
            BinaryPrimitives.WriteUInt16LittleEndian(bmp.AsSpan(26), 1); // 位面数
            BinaryPrimitives.WriteUInt16LittleEndian(bmp.AsSpan(28), (ushort)bitsPerPixel);
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(30), 0); // BI_RGB 不压缩
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(34), (uint)(stride * height));
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(38), 2835); // 72 DPI
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(42), 2835);
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(46), (uint)paletteColors);
            BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(50), 0);

            return bmp;
        }

        /// <summary>写 256 项灰度调色板（B = G = R = i）。</summary>
        private static void WriteGrayPalette(Span<byte> dst)
        {
            for (int i = 0; i < 256; i++)
            {
                dst[i * 4] = (byte)i;
                dst[i * 4 + 1] = (byte)i;
                dst[i * 4 + 2] = (byte)i;
                dst[i * 4 + 3] = 0;
            }
        }

        private static byte[] EncodeGray8(ReadOnlySpan<byte> pixels, int width, int height)
        {
            var bmp = CreateBmpHeader(width, height, bitsPerPixel: 8, paletteColors: 256, out int offset, out int stride);
            WriteGrayPalette(bmp.AsSpan(54, 1024));

            for (int y = 0; y < height; y++) // BMP 自下而上，逐行翻转
            {
                pixels.Slice((height - 1 - y) * width, width).CopyTo(bmp.AsSpan(offset + y * stride));
            }

            return bmp;
        }

        private static byte[] EncodeGray16(ReadOnlySpan<byte> pixels, int width, int height)
        {
            var bmp = CreateBmpHeader(width, height, bitsPerPixel: 8, paletteColors: 256, out int offset, out int stride);
            WriteGrayPalette(bmp.AsSpan(54, 1024));

            for (int y = 0; y < height; y++) // BMP 自下而上，逐行翻转
            {
                var src = pixels.Slice((height - 1 - y) * width * 2, width * 2);
                var dst = bmp.AsSpan(offset + y * stride);
                for (int x = 0; x < width; x++)
                {
                    dst[x] = src[x * 2 + 1]; // 左对齐 16 位容器的高字节
                }
            }

            return bmp;
        }

        private static byte[] EncodeRgb24(ReadOnlySpan<byte> pixels, int width, int height, bool swapRAndB)
        {
            var bmp = CreateBmpHeader(width, height, bitsPerPixel: 24, paletteColors: 0, out int offset, out int stride);

            for (int y = 0; y < height; y++) // BMP 自下而上，逐行翻转
            {
                var src = pixels.Slice((height - 1 - y) * width * 3, width * 3);
                var dst = bmp.AsSpan(offset + y * stride);
                for (int x = 0; x < width; x++)
                {
                    byte first = src[x * 3]; // RGB8 时为 R，BGR8 时为 B
                    byte third = src[x * 3 + 2];
                    dst[x * 3] = swapRAndB ? third : first; // BMP 像素为 BGR 序
                    dst[x * 3 + 1] = src[x * 3 + 1];
                    dst[x * 3 + 2] = swapRAndB ? first : third;
                }
            }

            return bmp;
        }

        /// <summary>拜耳阵列双线性去马赛克：R/B 取对角均值，G 取正交均值，边缘用重复像素。</summary>
        private static byte[] EncodeBayer(ReadOnlySpan<byte> pixels, int width, int height, PixelFormat format)
        {
            // 2×2 模板中 R 的位置（BayerRG8=RGGB、BayerGR8=GRBG、BayerGB8=GBRG、BayerBG8=BGGR），B 恒在对角
            int redRow = format is PixelFormat.BayerGB8 or PixelFormat.BayerBG8 ? 1 : 0;
            int redCol = format is PixelFormat.BayerGR8 or PixelFormat.BayerBG8 ? 1 : 0;

            var bmp = CreateBmpHeader(width, height, bitsPerPixel: 24, paletteColors: 0, out int offset, out int stride);

            for (int sy = 0; sy < height; sy++) // 按源行序解马赛克，写入时翻转
            {
                var dst = bmp.AsSpan(offset + (height - 1 - sy) * stride);
                int srcBase = sy * width;
                int yParity = sy & 1;
                int rowM = Math.Max(sy - 1, 0) * width;
                int rowP = Math.Min(sy + 1, height - 1) * width;

                for (int x = 0; x < width; x++)
                {
                    int xParity = x & 1;
                    bool isRed = yParity == redRow && xParity == redCol;
                    bool isBlue = yParity != redRow && xParity != redCol;
                    int xm = Math.Max(x - 1, 0);
                    int xp = Math.Min(x + 1, width - 1);
                    byte c = pixels[srcBase + x];

                    byte r, g, b;
                    if (isRed)
                    {
                        r = c;
                        g = (byte)((pixels[srcBase + xm] + pixels[srcBase + xp] + pixels[rowM + x] + pixels[rowP + x]) >> 2);
                        b = (byte)((pixels[rowM + xm] + pixels[rowM + xp] + pixels[rowP + xm] + pixels[rowP + xp]) >> 2);
                    }
                    else if (isBlue)
                    {
                        b = c;
                        g = (byte)((pixels[srcBase + xm] + pixels[srcBase + xp] + pixels[rowM + x] + pixels[rowP + x]) >> 2);
                        r = (byte)((pixels[rowM + xm] + pixels[rowM + xp] + pixels[rowP + xm] + pixels[rowP + xp]) >> 2);
                    }
                    else
                    {
                        g = c;
                        // 绿像素的左右/上下邻居分属 R、B，归属由模板行决定
                        if (yParity == redRow)
                        {
                            r = (byte)((pixels[srcBase + xm] + pixels[srcBase + xp]) >> 1);
                            b = (byte)((pixels[rowM + x] + pixels[rowP + x]) >> 1);
                        }
                        else
                        {
                            b = (byte)((pixels[srcBase + xm] + pixels[srcBase + xp]) >> 1);
                            r = (byte)((pixels[rowM + x] + pixels[rowP + x]) >> 1);
                        }
                    }

                    dst[x * 3] = b; // BMP 像素为 BGR 序
                    dst[x * 3 + 1] = g;
                    dst[x * 3 + 2] = r;
                }
            }

            return bmp;
        }
    }
}
