using System;
using System.Collections.Generic;
using Framework.Imaging;

namespace Framework.Detection
{
    /// <summary>
    /// 姿态估计能力契约：输入图像帧，输出人体关键点。与 <see cref="IDetector"/> 同层的能力契约——
    /// 同一个能力库、同一条引擎隔离边界（ADR-0007），调用方不感知具体推理引擎。
    /// 实现不保证线程安全：同一实例的 <see cref="Estimate"/> 须串行调用。
    /// </summary>
    public interface IPoseEstimator : IDisposable
    {
        /// <summary>
        /// 对一帧图像做姿态估计。阻塞至推理完成。
        /// 返回的人体外接矩形保证位于图像范围内（引擎的越界输出在实现内裁剪）。
        /// </summary>
        /// <param name="frame">输入图像帧（规范格式，见 Framework.Imaging）。</param>
        /// <param name="confidence">置信度阈值，低于该值的人体被丢弃。</param>
        /// <param name="iou">NMS 重叠阈值，框重叠超过该值被视为同一目标。</param>
        IReadOnlyList<PoseResult> Estimate(ImageFrame frame, double confidence = 0.25, double iou = 0.7);
    }
}
