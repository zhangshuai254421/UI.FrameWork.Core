using System;
using System.Collections.Generic;
using Framework.Imaging;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Models;

namespace Framework.Detection
{
    // 作者：Zhang Shuai
    // 描述：IObbDetector 的 YoloDotNet 实现——ONNX 模型 + CPU 执行提供方。
    //       引擎隔离边界（ADR-0007）：入参 ImageFrame（规范格式），出参 ObbResult；
    //       YoloDotNet 的类型（SKBitmap、OBBDetection）不越过这条边界。
    //       引擎怪癖翻译：引擎给的矩形是"未旋转矩形 + 绕中心弧度角"（其绘制即
    //       Translate(中心)→RotateRadians→DrawRect），本类按规则 6 只裁剪未旋转矩形
    //       回图像范围，角度原样透传；旋转后角点不裁剪（裁剪破坏四边形，见 ObbResult）。
    public sealed class YoloObbDetector : IObbDetector
    {
        private readonly Yolo _yolo;

        /// <summary>加载 ONNX 模型创建旋转框检测器。模型文件不存在或非法时抛异常。</summary>
        /// <param name="modelPath">ONNX 模型路径（必须是旋转框检测任务导出的模型）。</param>
        public YoloObbDetector(string modelPath)
        {
            _yolo = new Yolo(new YoloOptions
            {
                ExecutionProvider = new YoloDotNet.ExecutionProvider.Cpu.CpuExecutionProvider(model: modelPath),
            });
        }

        /// <inheritdoc/>
        public IReadOnlyList<ObbResult> Detect(ImageFrame frame, double confidence = 0.25, double iou = 0.7)
        {
            ArgumentNullException.ThrowIfNull(frame);
            using SKBitmap bitmap = SkBitmapConverter.Convert(frame);
            List<OBBDetection> results = _yolo.RunObbDetection(bitmap, confidence, iou);

            var detections = new List<ObbResult>(results.Count);
            foreach (OBBDetection result in results)
            {
                SKRectI box = result.BoundingBox;

                // 与 YoloDetector 同规：只裁未旋转矩形；角度绕矩形的（裁剪后）中心转。
                double x = Math.Max(0, box.Left);
                double y = Math.Max(0, box.Top);
                double right = Math.Min(frame.Width, box.Left + box.Width);
                double bottom = Math.Min(frame.Height, box.Top + box.Height);
                if (right <= x || bottom <= y)
                {
                    continue;
                }

                detections.Add(new ObbResult(
                    x, y, right - x, bottom - y, result.OrientationAngle,
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
