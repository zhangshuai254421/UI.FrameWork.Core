using System;
using System.Collections.Generic;
using Framework.Imaging;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Models;

namespace Framework.Detection
{
    // 作者：Zhang Shuai
    // 描述：IDetector 的 YoloDotNet 实现——ONNX 模型 + CPU 执行提供方。
    //       引擎隔离边界（ADR-0007）：入参 ImageFrame（规范格式），出参 Detection；
    //       YoloDotNet 的类型（SKBitmap、ObjectDetection）不越过这条边界。
    public sealed class YoloDetector : IDetector
    {
        private readonly Yolo _yolo;

        /// <summary>加载 ONNX 模型创建检测器。模型文件不存在或非法时抛异常。</summary>
        /// <param name="modelPath">ONNX 模型路径（必须是目标检测任务导出的模型）。</param>
        public YoloDetector(string modelPath)
        {
            _yolo = new Yolo(new YoloOptions
            {
                ExecutionProvider = new YoloDotNet.ExecutionProvider.Cpu.CpuExecutionProvider(model: modelPath),
            });
        }

        /// <inheritdoc/>
        public IReadOnlyList<Detection> Detect(ImageFrame frame, double confidence = 0.25, double iou = 0.7)
        {
            using SKBitmap bitmap = SkBitmapConverter.Convert(frame);
            List<ObjectDetection> results = _yolo.RunObjectDetection(bitmap, confidence, iou);

            var detections = new List<Detection>(results.Count);
            foreach (ObjectDetection result in results)
            {
                SKRectI box = result.BoundingBox;

                // 引擎在 letterbox 输入空间里预测，映射回原图的框可能带负坐标或越界；
                // 裁剪回图像范围，完全出界（裁剪后退化）的目标丢弃——下游只收界内框。
                double x = Math.Max(0, box.Left);
                double y = Math.Max(0, box.Top);
                double right = Math.Min(frame.Width, box.Left + box.Width);
                double bottom = Math.Min(frame.Height, box.Top + box.Height);
                if (right <= x || bottom <= y)
                {
                    continue;
                }

                detections.Add(new Detection(x, y, right - x, bottom - y,
                    result.Label.Name, result.Confidence));
            }

            return detections;
        }

        public void Dispose()
        {
            _yolo.Dispose();
        }
    }
}
