using System.Threading.Channels;

namespace Framework.Device.Domain
{
    /// <summary>
    /// 相机契约：强类型的最小相机能力，屏蔽厂商差异。
    /// 图像统一以 <see cref="CameraData"/> 规范表示，SDK 对象与裸指针不出契约层。
    /// </summary>
    public interface ICamera : IDevice
    {
        /// <summary>实时图像通道（连续预览），有界、丢最旧帧。</summary>
        Channel<CameraData> ChannelCameraData { get; }

        /// <summary>开始取流；未打开时记录错误并忽略。</summary>
        void StartAcquisition();

        /// <summary>停止取流；未打开时静默忽略。</summary>
        void StopAcquisition();

        /// <summary>取一帧图像；失败（未打开、超时等）返回空 <see cref="CameraData"/>（宽高为 0），不抛异常。</summary>
        CameraData GetOneImage();

        /// <summary>设置曝光时间（微秒）。</summary>
        void SetExposureTime(double exposureTimeUs);

        /// <summary>读取曝光时间（微秒）；读取失败返回 0。</summary>
        double GetExposureTime();

        /// <summary>设置增益（dB）。</summary>
        void SetGain(double gain);

        /// <summary>读取增益（dB）；读取失败返回 0。</summary>
        double GetGain();

        /// <summary>触发一次软触发；相机未处于软触发模式时由适配器记录错误。</summary>
        void TriggerSoftware();
    }
}
