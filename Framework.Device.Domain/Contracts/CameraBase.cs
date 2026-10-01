using System.Threading.Channels;

namespace Framework.Device.Domain
{
    /// <summary>
    /// 相机适配器基类：持有实时图像通道，并给出取流模式与图片保存的公共实现。
    /// 各厂商适配器只需实现抽象方法。
    /// </summary>
    public abstract class CameraBase : DeviceBase, ICamera
    {
        /// <summary>实时图像通道（连续预览）：有界容量 3、满时丢最旧帧；回调取流模式下由适配器持续写入。</summary>
        public Channel<CameraData> ChannelCameraData { get; } = Channel.CreateBounded<CameraData>(
            new BoundedChannelOptions(3)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

        /// <summary>取流模式（StartAcquisition 前设置）：Pull 主动拉帧；Callback 回调持续推帧进 <see cref="ChannelCameraData"/>。</summary>
        public CameraGrabMode GrabMode { get; set; } = CameraGrabMode.Pull;

        /// <summary>开始取流；未打开时记录错误并忽略。</summary>
        public abstract void StartAcquisition();

        /// <summary>停止取流；未打开时静默忽略。</summary>
        public abstract void StopAcquisition();

        /// <summary>取一帧图像；失败（未打开、超时等）返回空 <see cref="CameraData"/>（宽高为 0），不抛异常。</summary>
        public abstract CameraData GetOneImage();

        /// <summary>设置曝光时间（微秒）。</summary>
        public abstract void SetExposureTime(double exposureTimeUs);

        /// <summary>读取曝光时间（微秒）；读取失败返回 0。</summary>
        public abstract double GetExposureTime();

        /// <summary>设置增益（dB）。</summary>
        public abstract void SetGain(double gain);

        /// <summary>读取增益（dB）；读取失败返回 0。</summary>
        public abstract double GetGain();

        /// <summary>触发一次软触发；相机未处于软触发模式时由适配器记录错误。</summary>
        public abstract void TriggerSoftware();

        /// <summary>取一帧并保存为图片文件（默认 BMP 格式）；目标目录不存在自动创建，同名文件覆盖。取图为空帧或写盘失败返回 false。</summary>
        public bool SaveImage(string filePath, ImageFileFormat format = ImageFileFormat.Bmp)
        {
            return CameraImageFile.Save(GetOneImage(), filePath, format);
        }
    }
}
