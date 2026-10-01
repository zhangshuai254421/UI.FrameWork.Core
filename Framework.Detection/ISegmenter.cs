using System;
using System.Collections.Generic;
using Framework.Imaging;

namespace Framework.Detection
{
    /// <summary>
    /// 实例分割能力契约：输入图像帧，输出逐实例的外接矩形与前景掩码。
    /// 与 <see cref="IDetector"/>、<see cref="IPoseEstimator"/> 同层的能力契约——
    /// 同一个能力库、同一条引擎隔离边界（ADR-0007），调用方不感知具体推理引擎。
    /// 实现不保证线程安全：同一实例的 <see cref="Segment"/> 须串行调用。
    /// </summary>
    public interface ISegmenter : IDisposable
    {
        /// <summary>
        /// 对一帧图像做实例分割。阻塞至推理完成。
        /// 返回的外接矩形保证位于图像范围内；掩码与矩形同尺寸同原点。
        /// </summary>
        /// <param name="frame">输入图像帧（规范格式，见 Framework.Imaging）。</param>
        /// <param name="confidence">实例置信度阈值，低于该值的实例被丢弃。</param>
        /// <param name="pixelConfidence">像素置信度阈值，掩码中低于该值的像素不算前景。</param>
        /// <param name="iou">NMS 重叠阈值，框重叠超过该值被视为同一目标。</param>
        IReadOnlyList<SegmentationResult> Segment(
            ImageFrame frame,
            double confidence = 0.25,
            double pixelConfidence = 0.5,
            double iou = 0.7);
    }
}
