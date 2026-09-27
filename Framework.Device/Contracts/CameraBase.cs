using System.Threading.Channels;

namespace Framework.Device
{
    public abstract class CameraBase : DeviceBase, ICamera
    {
        public Channel<CameraData> ChannelCameraData { get; } = Channel.CreateBounded<CameraData>(
            new BoundedChannelOptions(3)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

        public abstract void StartAcquisition();

        public abstract void StopAcquisition();

        public abstract CameraData GetOneImage();

        public abstract void SetExposureTime(double exposureTimeUs);

        public abstract double GetExposureTime();

        public abstract void SetGain(double gain);

        public abstract double GetGain();

        public abstract void TriggerSoftware();

        /// <summary>取一帧并保存为图片文件（默认 BMP 格式）；目标目录不存在自动创建，同名文件覆盖。取图为空帧或写盘失败返回 false。</summary>
        public bool SaveImage(string filePath, ImageFileFormat format = ImageFileFormat.Bmp)
        {
            return CameraImageFile.Save(GetOneImage(), filePath, format);
        }
    }
}
