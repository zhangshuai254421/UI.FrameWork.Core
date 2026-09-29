using Framework.Device;
using Microsoft.Extensions.Logging;
using SemiControl.Controls;
using System;
using System.Collections.Concurrent;

namespace UI.FrameWork.Core.Imaging
{
    // 作者：Zhang Shuai
    // 描述：相机帧 → 图像帧的宿主侧桥接。设备层（Framework.Device）与控件库（SemiControl）
    //       互不引用（Docs/adr/0004），这条缝上的翻译只能住在同时看得到两者的宿主项目里。
    public static class CameraFrameMapper
    {
        // 同一格式只告警一次（应用域内），避免 30fps 刷日志。
        private static readonly ConcurrentDictionary<PixelFormat, byte> _warnedFormats = new();

        /// <summary>
        /// 把相机帧包装为图像帧。不拷贝像素——共享数据数组（ImageFrame 所有权契约：
        /// 控件转换进内部缓冲后即不再引用，见 Docs/adr/0004）。格式控件不支持（拜耳/未定义——
        /// 去马赛克是适配器职责）或数据非法时返回 null，并经 <paramref name="logger"/> 限频告警。
        /// </summary>
        public static ImageFrame? ToImageFrame(this CameraData data, ILogger? logger = null)
        {
            ImagePixelFormat? format = data.PixelFormat switch
            {
                PixelFormat.Mono8 => ImagePixelFormat.Mono8,
                // 10/12 位数据同为 16 位小端容器、数据左对齐，按 Mono16 交给控件归一化显示。
                PixelFormat.Mono10 or PixelFormat.Mono12 or PixelFormat.Mono16 => ImagePixelFormat.Mono16,
                PixelFormat.RGB8 => ImagePixelFormat.RGB8,
                PixelFormat.BGR8 => ImagePixelFormat.BGR8,
                _ => null,
            };

            if (format is null)
            {
                WarnUnsupported(data.PixelFormat, logger);
                return null;
            }

            try
            {
                return new ImageFrame(data.Width, data.Height, format.Value, data.ImageData);
            }
            catch (ArgumentException ex)
            {
                logger?.LogWarning(ex, "相机帧非法被丢弃（{Width}×{Height}，{Format}）",
                    data.Width, data.Height, data.PixelFormat);
                return null;
            }
        }

        private static void WarnUnsupported(PixelFormat format, ILogger? logger)
        {
            if (logger is null || !_warnedFormats.TryAdd(format, 0))
            {
                return;
            }

            logger.LogWarning("相机帧格式 {Format} 不受支持（拜耳/未定义——去马赛克是适配器职责），已丢弃", format);
        }
    }
}
