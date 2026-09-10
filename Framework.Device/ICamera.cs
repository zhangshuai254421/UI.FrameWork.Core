using System.Threading.Channels;

namespace Framework.Device
{
    /// <summary>
    /// 相机契约：强类型的最小相机能力，屏蔽厂商差异。
    /// </summary>
    public interface ICamera : IDevice
    {
        /// <summary>实时图像通道（连续预览），有界、丢最旧帧。</summary>
        Channel<CameraData> ChannelCameraData { get; }

        void StartAcquisition();

        void StopAcquisition();

        CameraData GetOneImage();

        void SetExposureTime(double exposureTimeUs);

        double GetExposureTime();

        void SetGain(double gain);

        double GetGain();

        void TriggerSoftware();
    }
}
