using System.Threading.Channels;

namespace Framework.Device
{
    public abstract class CameraBase : DeviceBase,ICamera
    {
        public Channel<CameraData> ChannelCameraData { get; set; } = Channel.CreateBounded<CameraData>(
        new BoundedChannelOptions(3)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        public abstract float GetExposureTime();
        public abstract void GetFloatValue(string strKey, ref float nValue);
        public abstract CameraData GetOneImage();
        public abstract void SetCommandValue(string CommandValue);
        public abstract void SetEnumValue(string strKey, uint nValue);
        public abstract void SetFloatValue(string strKey, float nValue);
        public abstract void SetUserSetDefault();
        public abstract void SetUserSetSelector();
        public abstract void TriggerSoftware();
        public abstract long UserSetCurrent();
        public abstract void UserSetLoad();
        public abstract void UserSetSave();
    }
}
