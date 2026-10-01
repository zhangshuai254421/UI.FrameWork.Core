using System;
using System.Collections.Generic;
using Framework.Imaging;

namespace Framework.Detection
{
    /// <summary>
    /// 检测能力契约：输入图像帧，输出目标框列表。调用方不感知具体推理引擎
    /// （YoloDotNet、ONNX Runtime……），更换引擎不波及页面与宿主代码。
    /// 实现不保证线程安全：同一实例的 <see cref="Detect"/> 须串行调用。
    /// </summary>
    public interface IDetector : IDisposable
    {
        /// <summary>
        /// 对一帧图像做目标检测。阻塞至推理完成，耗时取决于模型与图像尺寸。
        /// 返回的框保证位于图像范围内（引擎的越界输出在实现内裁剪）。
        /// </summary>
        /// <param name="frame">输入图像帧（规范格式，见 Framework.Imaging）。</param>
        /// <param name="confidence">置信度阈值，低于该值的目标被丢弃。</param>
        /// <param name="iou">NMS 重叠阈值，框重叠超过该值被视为同一目标。</param>
        IReadOnlyList<Detection> Detect(ImageFrame frame, double confidence = 0.25, double iou = 0.7);
    }
}
