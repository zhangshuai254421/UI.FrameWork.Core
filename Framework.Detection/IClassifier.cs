using System;
using System.Collections.Generic;
using Framework.Imaging;

namespace Framework.Detection
{
    /// <summary>
    /// 图像分类能力契约：输入图像帧，输出整图的 Top-K 类别候选。
    /// 与 <see cref="IDetector"/> 等同层的能力契约——同一个能力库、同一条引擎隔离边界
    /// （ADR-0007），调用方不感知具体推理引擎。
    /// 实现不保证线程安全：同一实例的 <see cref="Classify"/> 须串行调用。
    /// </summary>
    public interface IClassifier : IDisposable
    {
        /// <summary>对一帧图像做分类。阻塞至推理完成。返回按置信度降序的前 K 个候选。</summary>
        /// <param name="frame">输入图像帧（规范格式，见 Framework.Imaging）。</param>
        /// <param name="classes">取前几个候选（Top-K），1 即只取最优类别。</param>
        IReadOnlyList<ClassificationResult> Classify(ImageFrame frame, int classes = 1);
    }
}
