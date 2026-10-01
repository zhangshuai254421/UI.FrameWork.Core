using System;
using System.Collections.Generic;
using Framework.Imaging;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Models;

namespace Framework.Detection
{
    // 作者：Zhang Shuai
    // 描述：ISegmenter 的 YoloDotNet 实现——ONNX 模型 + CPU 执行提供方。
    //       引擎隔离边界（ADR-0007）：入参 ImageFrame（规范格式），出参 SegmentationResult；
    //       YoloDotNet 的类型（SKBitmap、Segmentation）不越过这条边界。
    //       引擎怪癖在本类内翻译完：框裁剪回图像范围；掩码按未裁剪外接框位打包
    //       （行优先、LSB-first），此处裁剪平移到与结果框精确对齐，下游拿到即用。
    public sealed class YoloSegmenter : ISegmenter
    {
        private readonly Yolo _yolo;

        /// <summary>加载 ONNX 模型创建分割器。模型文件不存在或非法时抛异常。</summary>
        /// <param name="modelPath">ONNX 模型路径（必须是实例分割任务导出的模型）。</param>
        public YoloSegmenter(string modelPath)
        {
            _yolo = new Yolo(new YoloOptions
            {
                ExecutionProvider = new YoloDotNet.ExecutionProvider.Cpu.CpuExecutionProvider(model: modelPath),
            });
        }

        /// <inheritdoc/>
        public IReadOnlyList<SegmentationResult> Segment(
            ImageFrame frame,
            double confidence = 0.25,
            double pixelConfidence = 0.5,
            double iou = 0.7)
        {
            ArgumentNullException.ThrowIfNull(frame);
            using SKBitmap bitmap = SkBitmapConverter.Convert(frame);
            List<Segmentation> results = _yolo.RunSegmentation(bitmap, confidence, pixelConfidence, iou);

            var segmentations = new List<SegmentationResult>(results.Count);
            foreach (Segmentation result in results)
            {
                SKRectI box = result.BoundingBox;
                byte[]? packed = result.BitPackedPixelMask;
                if (packed is null)
                {
                    continue;
                }

                // 与 YoloDetector 同规：引擎在 letterbox 输入空间里预测，映射回原图的框
                // 可能带负坐标或越界；裁剪回图像范围，完全出界（裁剪后退化）的实例丢弃。
                double x = Math.Max(0, box.Left);
                double y = Math.Max(0, box.Top);
                double right = Math.Min(frame.Width, box.Left + box.Width);
                double bottom = Math.Min(frame.Height, box.Top + box.Height);
                if (right <= x || bottom <= y)
                {
                    continue;
                }

                int width = (int)(right - x);
                int height = (int)(bottom - y);

                // 掩码按未裁剪外接框打包，裁掉落在图像外的行列，使其与结果框同尺寸同原点。
                byte[] cropped = CropBitPackedMask(
                    packed, box.Width, box.Height, (int)(x - box.Left), (int)(y - box.Top), width, height);

                segmentations.Add(new SegmentationResult(
                    x, y, width, height, result.Label.Name, result.Confidence,
                    new SegmentMask(width, height, cropped)));
            }

            return segmentations;
        }

        /// <summary>
        /// 从位打包掩码（行优先、每字节最低位在前）中裁出子矩形并重新打包。
        /// </summary>
        private static byte[] CropBitPackedMask(
            byte[] source, int sourceWidth, int sourceHeight, int cropX, int cropY, int cropWidth, int cropHeight)
        {
            if (cropX == 0 && cropY == 0 && cropWidth == sourceWidth && cropHeight == sourceHeight)
            {
                // 未越界常态：整块沿用，不重打包。
                return source;
            }

            var cropped = new byte[((cropWidth * cropHeight) + 7) / 8];
            for (int dy = 0; dy < cropHeight; dy++)
            {
                int sourceBase = ((cropY + dy) * sourceWidth) + cropX;
                int targetBase = dy * cropWidth;
                for (int dx = 0; dx < cropWidth; dx++)
                {
                    int sourceIndex = sourceBase + dx;
                    if ((source[sourceIndex >> 3] & (1 << (sourceIndex & 7))) != 0)
                    {
                        int targetIndex = targetBase + dx;
                        cropped[targetIndex >> 3] |= (byte)(1 << (targetIndex & 7));
                    }
                }
            }

            return cropped;
        }

        public void Dispose()
        {
            _yolo.Dispose();
        }
    }
}
