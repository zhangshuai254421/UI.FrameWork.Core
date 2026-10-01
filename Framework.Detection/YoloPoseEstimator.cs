using System;
using System.Collections.Generic;
using Framework.Imaging;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Models;

namespace Framework.Detection
{
    // 作者：Zhang Shuai
    // 描述：IPoseEstimator 的 YoloDotNet 实现——ONNX 姿态模型 + CPU 执行提供方。
    //       与 YoloDetector 同一条引擎隔离边界（ADR-0007）：入参 ImageFrame，
    //       出参 PoseResult；引擎类型不越过边界。
    public sealed class YoloPoseEstimator : IPoseEstimator
    {
        /// <summary>
        /// COCO 17 关键点拓扑（骨架连线），对应 Ultralytics COCO 预训练姿态模型的输出顺序。
        /// 这是模型知识而非引擎知识：换自定义关键点拓扑的模型时需同步替换。
        /// </summary>
        public static readonly int[][] CocoSkeletonEdges =
        {
            new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 3 }, new[] { 2, 4 },        // 头部
            new[] { 5, 6 },                                                        // 肩线
            new[] { 5, 7 }, new[] { 7, 9 }, new[] { 6, 8 }, new[] { 8, 10 },       // 手臂
            new[] { 5, 11 }, new[] { 6, 12 }, new[] { 11, 12 },                    // 躯干
            new[] { 11, 13 }, new[] { 13, 15 }, new[] { 12, 14 }, new[] { 14, 16 },// 腿
        };

        private readonly Yolo _yolo;

        /// <summary>加载 ONNX 姿态模型创建估计器。模型文件不存在、非法或不是姿态任务导出时抛异常。</summary>
        /// <param name="modelPath">ONNX 模型路径（必须是姿态估计任务导出的模型）。</param>
        public YoloPoseEstimator(string modelPath)
        {
            _yolo = new Yolo(new YoloOptions
            {
                ExecutionProvider = new YoloDotNet.ExecutionProvider.Cpu.CpuExecutionProvider(model: modelPath),
            });
        }

        /// <inheritdoc/>
        public IReadOnlyList<PoseResult> Estimate(ImageFrame frame, double confidence = 0.25, double iou = 0.7)
        {
            using SKBitmap bitmap = SkBitmapConverter.Convert(frame);
            List<PoseEstimation> results = _yolo.RunPoseEstimation(bitmap, confidence, iou);

            var poses = new List<PoseResult>(results.Count);
            foreach (PoseEstimation result in results)
            {
                SKRectI box = result.BoundingBox;

                // 与检测同规则：letterbox 映射可能越界，外接框裁剪回图像范围。
                double x = Math.Max(0, box.Left);
                double y = Math.Max(0, box.Top);
                double right = Math.Min(frame.Width, box.Left + box.Width);
                double bottom = Math.Min(frame.Height, box.Top + box.Height);
                if (right <= x || bottom <= y)
                {
                    continue;
                }

                // 关键点坐标保留原值（部位遮挡或延伸出画面时允许出界，显示侧自行裁剪）。
                var keyPoints = new List<KeyPoint>(result.KeyPoints.Length);
                foreach (YoloDotNet.Models.KeyPoint keyPoint in result.KeyPoints)
                {
                    keyPoints.Add(new KeyPoint(keyPoint.X, keyPoint.Y, keyPoint.Confidence));
                }

                poses.Add(new PoseResult(x, y, right - x, bottom - y,
                    result.Label.Name, result.Confidence, keyPoints));
            }

            return poses;
        }

        public void Dispose()
        {
            _yolo.Dispose();
        }
    }
}
