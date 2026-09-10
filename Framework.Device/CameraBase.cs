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
    }
}
