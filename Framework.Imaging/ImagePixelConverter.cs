// Controls/ImageViewer/ImagePixelConverter.cs
using System;

namespace SemiControl.Controls
{
    /// <summary>
    /// 像素格式转换：把 <see cref="ImageFrame"/> 支持的各格式翻译为控件渲染用的 Bgra32。
    /// 纯函数实现，不含任何 WPF 类型，便于单元测试。
    /// </summary>
    public static class ImagePixelConverter
    {
        /// <summary>取指定格式的每像素字节数。</summary>
        public static int GetBytesPerPixel(ImagePixelFormat pixelFormat) => pixelFormat switch
        {
            ImagePixelFormat.Mono8 => 1,
            ImagePixelFormat.Mono16 => 2,
            ImagePixelFormat.RGB8 => 3,
            ImagePixelFormat.BGR8 => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, "未知的像素格式。"),
        };

        /// <summary>
        /// 把整帧转换为 Bgra32（每像素 4 字节，字节序 B-G-R-A）写入 <paramref name="destination"/>。
        /// 帧不合法或目标缓冲不足时返回 false（不抛异常，调用方沿用上一帧画面）。
        /// </summary>
        public static bool TryConvertToBgra32(ImageFrame? frame, Span<byte> destination)
        {
            if (frame is null || destination.Length < frame.Width * frame.Height * 4)
            {
                return false;
            }

            var pixels = frame.Data.AsSpan();
            int pixelCount = frame.Width * frame.Height;

            switch (frame.PixelFormat)
            {
                case ImagePixelFormat.Mono8:
                {
                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte v = pixels[i];
                        int o = i * 4;
                        destination[o] = v;
                        destination[o + 1] = v;
                        destination[o + 2] = v;
                        destination[o + 3] = 255;
                    }

                    break;
                }

                case ImagePixelFormat.Mono16:
                {
                    // 与 Framework.Device 落盘约定一致：小端容器、高字节为有效位（显示取高 8 位）。
                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte v = pixels[i * 2 + 1];
                        int o = i * 4;
                        destination[o] = v;
                        destination[o + 1] = v;
                        destination[o + 2] = v;
                        destination[o + 3] = 255;
                    }

                    break;
                }

                case ImagePixelFormat.RGB8:
                {
                    for (int i = 0; i < pixelCount; i++)
                    {
                        int s = i * 3;
                        int o = i * 4;
                        destination[o] = pixels[s + 2];
                        destination[o + 1] = pixels[s + 1];
                        destination[o + 2] = pixels[s];
                        destination[o + 3] = 255;
                    }

                    break;
                }

                case ImagePixelFormat.BGR8:
                {
                    for (int i = 0; i < pixelCount; i++)
                    {
                        int s = i * 3;
                        int o = i * 4;
                        destination[o] = pixels[s];
                        destination[o + 1] = pixels[s + 1];
                        destination[o + 2] = pixels[s + 2];
                        destination[o + 3] = 255;
                    }

                    break;
                }

                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 读取一个像素的显示颜色（已归一化为 8 位/通道：Mono16 取高 8 位，灰度三通道同值）。
        /// 坐标越界返回 false，用于鼠标处像素信息条。
        /// </summary>
        public static bool TryGetDisplayColor(ImageFrame? frame, int x, int y, out byte r, out byte g, out byte b)
        {
            r = g = b = 0;
            if (frame is null || x < 0 || y < 0 || x >= frame.Width || y >= frame.Height)
            {
                return false;
            }

            int pixelIndex = y * frame.Width + x;
            var pixels = frame.Data;

            switch (frame.PixelFormat)
            {
                case ImagePixelFormat.Mono8:
                    r = g = b = pixels[pixelIndex];
                    break;
                case ImagePixelFormat.Mono16:
                    r = g = b = pixels[pixelIndex * 2 + 1];
                    break;
                case ImagePixelFormat.RGB8:
                    r = pixels[pixelIndex * 3];
                    g = pixels[pixelIndex * 3 + 1];
                    b = pixels[pixelIndex * 3 + 2];
                    break;
                case ImagePixelFormat.BGR8:
                    b = pixels[pixelIndex * 3];
                    g = pixels[pixelIndex * 3 + 1];
                    r = pixels[pixelIndex * 3 + 2];
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}
