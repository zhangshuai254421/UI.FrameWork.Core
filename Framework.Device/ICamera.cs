using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Framework.Device
{

    public interface  ICamera: IDevice 
    {
        //bool Open(string? serialNumber);
        //设置相机的枚举类型的参数
        public  void SetEnumValue(string strKey, uint nValue);

        public  void SetCommandValue(string CommandValue);
        public  void TriggerSoftware();
        public  void GetFloatValue(string strKey, ref float nValue);

        public  void SetFloatValue(string strKey, float nValue);

        public Channel<CameraData> ChannelCameraData { get; set; }
        public CameraData GetOneImage();

        public  float GetExposureTime();

        public  void SetUserSetSelector();
        public  void SetUserSetDefault();
        public  void UserSetLoad();
        public  void UserSetSave();
        public  long UserSetCurrent();

    }
}
