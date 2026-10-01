using System;
using System.Runtime.InteropServices;
using Framework.Imaging;
using SkiaSharp;

namespace Framework.Detection
{
    /// <summary>
    /// 图像帧 → SKBitmap 的内部转换器：推理引擎吃 SKBitmap，能力库对外只认 ImageFrame。
    /// 被检测与姿态估计两个估计器共用。目标格式按源格式就近选择：
    /// Mono8 → Gray8（逐字节）、Mono16 → Gray8（取高字节）、RGB8/BGR8 → 32 位（补 1 字节）。
    /// </summary>
    internal static class SkBitmapConverter
    {
        /// <summary>把图像帧转换为推理引擎所需的 SKBitmap：按源像素格式选择 SkiaSharp 色彩类型并拷入原生缓冲；不支持的格式抛异常。</summary>
        public static SKBitmap Convert(ImageFrame frame)
        {
            int pixelCount = frame.Width * frame.Height;
            byte[] pixels = frame.PixelFormat switch
            {
                // Mono8 与 Gray8 内存布局一致，直接拷入原生缓冲。
                ImagePixelFormat.Mono8 => frame.Data,
                // 16 位灰度数据左对齐（小端高字节在前），取高字节近似为 8 位灰度。
                ImagePixelFormat.Mono16 => TakeHighBytes(frame.Data, pixelCount),
                // 24 位彩色补一个字节扩成 32 位：RGB8 → Rgb888x（x 字节不参与），
                // BGR8 → Bgra8888（alpha 必须不透明，否则整图被当透明处理）。
                ImagePixelFormat.RGB8 => ExpandTo32(frame.Data, pixelCount, rgbFirst: true, alpha: 0),
                ImagePixelFormat.BGR8 => ExpandTo32(frame.Data, pixelCount, rgbFirst: false, alpha: 255),
                _ => throw new NotSupportedException($"不支持的像素格式：{frame.PixelFormat}"),
            };

            var info = new SKImageInfo(frame.Width, frame.Height, ToColorType(frame.PixelFormat));
            var bitmap = new SKBitmap(info);
            Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixelCount * info.BytesPerPixel);
            return bitmap;
        }

        /// <summary>把库内像素格式映射为 SkiaSharp 对应的色彩类型；未知格式抛 NotSupportedException。</summary>
        private static SKColorType ToColorType(ImagePixelFormat format)
        {
            return format switch
            {
                ImagePixelFormat.Mono8 => SKColorType.Gray8,
                ImagePixelFormat.Mono16 => SKColorType.Gray8,
                ImagePixelFormat.RGB8 => SKColorType.Rgb888x,
                ImagePixelFormat.BGR8 => SKColorType.Bgra8888,
                _ => throw new NotSupportedException($"不支持的像素格式：{format}"),
            };
        }

        /// <summary>从 16 位小端灰度数据取每像素高字节，缩为 8 位灰度数组；返回长度为 pixelCount 的新数组。</summary>
        private static byte[] TakeHighBytes(byte[] source, int pixelCount)
        {
            var target = new byte[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                target[i] = source[(i * 2) + 1];
            }

            return target;
        }

        /// <summary>把每像素 3 字节的彩色数据扩为每像素 4 字节：rgbFirst 为真按 R-G-B 取序，否则按 B-G-R；每像素第 4 字节写入 alpha。</summary>
        private static byte[] ExpandTo32(byte[] source, int pixelCount, bool rgbFirst, byte alpha)
        {
            var target = new byte[pixelCount * 4];
            for (int i = 0; i < pixelCount; i++)
            {
                int s = i * 3;
                int d = i * 4;
                target[d] = rgbFirst ? source[s] : source[s + 2];
                target[d + 1] = source[s + 1];
                target[d + 2] = rgbFirst ? source[s + 2] : source[s];
                target[d + 3] = alpha;
            }

            return target;
        }
    }
}
