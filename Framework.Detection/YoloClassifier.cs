using System;
using System.Collections.Generic;
using Framework.Imaging;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Models;

namespace Framework.Detection
{
    // 作者：Zhang Shuai
    // 描述：IClassifier 的 YoloDotNet 实现——ONNX 模型 + CPU 执行提供方。
    //       引擎隔离边界（ADR-0007）：入参 ImageFrame（规范格式），出参 ClassificationResult；
    //       YoloDotNet 的类型（SKBitmap、Classification）不越过这条边界。
    public sealed class YoloClassifier : IClassifier
    {
        private readonly Yolo _yolo;

        /// <summary>加载 ONNX 模型创建分类器。模型文件不存在或非法时抛异常。</summary>
        /// <param name="modelPath">ONNX 模型路径（必须是图像分类任务导出的模型）。</param>
        public YoloClassifier(string modelPath)
        {
            _yolo = new Yolo(new YoloOptions
            {
                ExecutionProvider = new YoloDotNet.ExecutionProvider.Cpu.CpuExecutionProvider(model: modelPath),
            });
        }

        /// <inheritdoc/>
        public IReadOnlyList<ClassificationResult> Classify(ImageFrame frame, int classes = 1)
        {
            ArgumentNullException.ThrowIfNull(frame);
            using SKBitmap bitmap = SkBitmapConverter.Convert(frame);
            List<Classification> results = _yolo.RunClassification(bitmap, classes);

            var classifications = new List<ClassificationResult>(results.Count);
            foreach (Classification result in results)
            {
                classifications.Add(new ClassificationResult(result.Label, result.Confidence));
            }

            return classifications;
        }

        public void Dispose()
        {
            _yolo.Dispose();
        }
    }
}
