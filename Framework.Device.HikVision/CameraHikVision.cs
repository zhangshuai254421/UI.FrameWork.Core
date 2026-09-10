using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MvCameraControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static MvCamCtrl.NET.MyCamera;

namespace Framework.Device.HikVision
{
    public class CameraHikVision : CameraBase
    {

        private readonly ILogger<CameraHikVision> _logger;

        public CameraHikVision(ILogger<CameraHikVision>? logger = null)
        {
            _logger = logger ?? NullLogger<CameraHikVision>.Instance;
            SDKSystem.Initialize();
        }


        public override string Name => throw new NotImplementedException();

        /// <summary>
        /// 注册回调函数
        /// </summary>
        public void RegisterImageCallback()
        {
            this.StopAcquisition();
            CCamera.StreamGrabber.FrameGrabedEventEx += FrameGrabedEventHandler;
            this.StartAcquisition();
        }


        static void FrameGrabedEventHandler(object sender, FrameGrabbedEventArgs e)
        {
            Console.WriteLine("Get one frame: Width[{0}] , Height[{1}] , ImageSize[{2}], FrameNum[{3}]", e.FrameOut.Image.Width, e.FrameOut.Image.Height, e.FrameOut.Image.ImageSize, e.FrameOut.FrameNum);

            // ch: 手动释放缓存 | en: Dispose frame manually
            e.FrameOut.Dispose();

        }
        /// <summary>
        /// 停止采集
        /// </summary>
        public void StopAcquisition()
        {
            nRet = CCamera.StreamGrabber.StopGrabbing();
        }

        /// <summary>
        /// 开始采集
        /// </summary>
        public void StartAcquisition()
        {
            nRet = CCamera.StreamGrabber.StartGrabbing();
        }


        public override float GetExposureTime()
        {
            throw new NotImplementedException();
        }

        public override void GetFloatValue(string strKey, ref float nValue)
        {
            throw new NotImplementedException();
        }

        public override CameraData GetOneImage()
        {
            throw new NotImplementedException();
        }

        public override bool Open()
        {
            //枚举网络上的设备
            CheckFuncReturnCode(FindCameraDevice(), nameof(FindCameraDevice));
            //显示-选择-打开-相机设备
            ShowAndChooseAndOpenCameraDevice(string.Empty);

            //探测网络包最佳大小
            GetOptimalPacketSize();

            //设置相机的采集模式
            SetAcquisitionMode();

            //设置相机的触发模式
            //SetTriggerMode();

            ////触发模式打开设置触发源才又意义
            //SetTriggerSource();

            //注册取图回调函数
            RegisterImageCallback();
            return true;
        }

        public override void SetCommandValue(string CommandValue)
        {
            throw new NotImplementedException();
        }

        public override void SetEnumValue(string strKey, uint nValue)
        {
            throw new NotImplementedException();
        }

        public override void SetFloatValue(string strKey, float nValue)
        {
            throw new NotImplementedException();
        }

        public override void SetUserSetDefault()
        {
            throw new NotImplementedException();
        }

        public override void SetUserSetSelector()
        {
            throw new NotImplementedException();
        }

        public override void TriggerSoftware()
        {
            throw new NotImplementedException();
        }

        public override long UserSetCurrent()
        {
            throw new NotImplementedException();
        }

        public override void UserSetLoad()
        {
            throw new NotImplementedException();
        }

        public override void UserSetSave()
        {
            throw new NotImplementedException();
        }
       

        #region MyRegion
        int nRet = MvError.MV_OK;
        //网络中枚举的设备信息
        List<IDeviceInfo> deviceInfoList = new List<IDeviceInfo>();

        /// <summary>
        /// 相机信息
        /// </summary>
        public IDeviceInfo deviceInfo ;
        /// <summary>
        /// 相机控制类
        /// </summary>
        public MvCameraControl.IDevice CCamera ;

        public  Action<string>? LoggerAction;

        ///// <summary>
        ///// 枚举参数信息
        ///// </summary>
        //CEnumValue cEnumValue = new CEnumValue();
        //CEnumEntry cEnumEntry = new CEnumEntry();
 

        /// <summary>
        /// 设置相机触发源
        /// </summary>
        private void SetTriggerSource()
        {
            CheckFuncReturnCode(CCamera.Parameters.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE), nameof(CCamera.Parameters.SetEnumValue));
        }

        /// <summary>
        /// 设置相机的触发模式
        /// </summary>
        private void SetTriggerMode()
        {
            CheckFuncReturnCode(CCamera.Parameters.SetEnumValue("TriggerMode", (uint)MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON), nameof(CCamera.Parameters.SetEnumValue));

        }

        public DriverLibResult CheckFuncReturnCode(int returnCode, string funcName)
        {
            DriverLibResult driverLibResult = new DriverLibResult();

            if (returnCode ==0)
            {
                return DriverLibResult.DriverLibNoError;
            }
            else
            {

                HikErrorCode hikErrorCode = (HikErrorCode)returnCode;

                return DriverLibResult.DriverLibError;
            }
        }


        /// <summary>
        /// 设置相机采集模式
        /// </summary>
        private void SetAcquisitionMode()
        {

            CheckFuncReturnCode(CCamera.Parameters.SetEnumValue("AcquisitionMode", (uint)MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS), nameof(CCamera.Parameters.GetEnumValue));
        }

        /// <summary>
        /// 探测网络包最佳大小
        /// </summary>
        private void GetOptimalPacketSize()
        {
            // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
            if (CCamera is IGigEDevice)
            {
                //ch: 转换为gigE设备 | en: Convert to Gige device
                IGigEDevice gigEDevice = CCamera as IGigEDevice;
                int optionPacketSize;
                int result = gigEDevice.GetOptimalPacketSize(out optionPacketSize);
                if (result > 0)
                {
                    result = CCamera.Parameters.SetIntValue("GevSCPSPacketSize", (long)optionPacketSize);

                }
                else
                {
                    //logger.LogError($"Warning: Get Packet Size failed:{nRet:x8}");
                }
            }
        }

        //设备打开状态
        bool m_bIsDeviceOpen = false;

      
        /// <summary>
        /// 显示-选择-打开-相机设备
        /// </summary>
        /// <param name="SerialNumber"></param>
        private void ShowAndChooseAndOpenCameraDevice(string? SerialNumber)
        {

            //ch: 创建设备 | en: Create device
            //CCamera = DeviceFactory.CreateDeviceByIp(deviceIp, netExport);


            if (string.IsNullOrEmpty(SerialNumber))
            {
                deviceInfo = deviceInfoList[0];
                //创建设备 

                CCamera = DeviceFactory.CreateDevice(deviceInfo);
                

                CheckFuncReturnCode(CCamera.Open(), nameof(CCamera.Open));
                m_bIsDeviceOpen = true;
                return;
            }

            for (int i = 0; i < deviceInfoList.Count; i++)
            {
                if (deviceInfoList[i] is IGigEDevice)
                {
                    IGigEDevice gigEDevice = deviceInfoList[i] as IGigEDevice;

                    IGigEDeviceInfo gigeDevInfo = deviceInfo as IGigEDeviceInfo;
                    uint nIp1 = ((gigeDevInfo.CurrentIp & 0xff000000) >> 24);
                    uint nIp2 = ((gigeDevInfo.CurrentIp & 0x00ff0000) >> 16);
                    uint nIp3 = ((gigeDevInfo.CurrentIp & 0x0000ff00) >> 8);
                    uint nIp4 = (gigeDevInfo.CurrentIp & 0x000000ff);
                    Console.WriteLine("DevIP: {0}.{1}.{2}.{3}", nIp1, nIp2, nIp3, nIp4);

                    //uint nIp1 = (gigEDevice. & 0xff000000) >> 24;
                    //uint nIp2 = (cGigEDeviceInfo.nCurrentIp & 0x00ff0000) >> 16;
                    //uint nIp3 = (cGigEDeviceInfo.nCurrentIp & 0x0000ff00) >> 8;
                    //uint nIp4 = cGigEDeviceInfo.nCurrentIp & 0x000000ff;
                    ////logger.LogDebug($"Device{i.ToString()}: DevIP:{nIp1}.{nIp2}.{nIp3}.{nIp4}");
                    //if ("" != cGigEDeviceInfo.UserDefinedName)
                    //{
                    //    //logger.LogError($"UserDefineName:{cGigEDeviceInfo.UserDefinedName}");
                    //}
                    //else
                    //{
                    //    //logger.LogDebug($"chUserDefinedName:{cGigEDeviceInfo.chUserDefinedName}");
                    //}

                    if (SerialNumber == deviceInfoList[i].SerialNumber)
                    {
                        deviceInfo = deviceInfoList[i];
                        //创建设备 

                        CCamera = DeviceFactory.CreateDevice(deviceInfo);
                        CheckFuncReturnCode(CCamera.Open(), nameof(CCamera.Open));
                        m_bIsDeviceOpen = true;
                    }

                }
                //else if (CSystem.MV_USB_DEVICE == deviceInfoList[i].nTLayerType)
                //{
                //    CUSBCameraInfo cUsb3DeviceInfo = (CUSBCameraInfo)deviceInfoList[i];
                //    //logger.LogDebug($"device :{cUsb3DeviceInfo.chSerialNumber}");
                //    //if ("" != cUsb3DeviceInfo.UserDefinedName)
                //    //{
                //    //    logger.LogError($"UserDefineName:{cUsb3DeviceInfo.UserDefinedName}");
                //    //}
                //    //else
                //    //{
                //    //    logger.LogDebug($"chUserDefinedName:{cUsb3DeviceInfo.chUserDefinedName}");
                //    //}
                //    if (SerialNumber == cUsb3DeviceInfo.chSerialNumber)
                //    {
                //        deviceInfo = deviceInfoList[i];
                //        //创建设备 
                //        CheckFuncReturnCode(CCamera.CreateHandle(ref deviceInfo), nameof(CCamera.CreateHandle));
                        
                //        //打开设备
                //        CheckFuncReturnCode(CCamera.OpenDevice(),nameof(CCamera.OpenDevice));
         

                //        m_bIsDeviceOpen = true;
                //    }
                //}
            }
        }


        /// <summary>
        /// 枚举网络上的设备
        /// </summary>
        /// <returns></returns>
        private int FindCameraDevice()
        {
            return DeviceEnumerator.EnumDevices(DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
            | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice | DeviceTLayerType.MvGenTLCameraLinkDevice | 
            DeviceTLayerType.MvGenTLXoFDevice, out deviceInfoList);

     
        }

        public override void Close()
        {
            var ret=CCamera.StreamGrabber.StopGrabbing();
            if (ret != MvError.MV_OK)
            {
                Console.WriteLine("Stop grabbing failed:{0:x8}", ret);
                return;
            }
            // ch:关闭设备 | en:Close device
            ret = CCamera.Close();
            if (ret != MvError.MV_OK)
            {
                Console.WriteLine("Close device failed:{0:x8}", ret);
                return;
            }

            // ch:销毁设备 | en:Destroy device
            CCamera.Dispose();
            CCamera = null;
        }

        private enum HikErrorCode
        {
            // Token: 0x0400001D RID: 29
            MV_OK,
            // Token: 0x0400001E RID: 30
            MV_E_HANDLE = -2147483648,
            // Token: 0x0400001F RID: 31
            MV_E_SUPPORT,
            // Token: 0x04000020 RID: 32
            MV_E_BUFOVER,
            // Token: 0x04000021 RID: 33
            MV_E_CALLORDER,
            // Token: 0x04000022 RID: 34
            MV_E_PARAMETER,
            // Token: 0x04000023 RID: 35
            MV_E_RESOURCE = -2147483642,
            // Token: 0x04000024 RID: 36
            MV_E_NODATA,
            // Token: 0x04000025 RID: 37
            MV_E_PRECONDITION,
            // Token: 0x04000026 RID: 38
            MV_E_VERSION,
            // Token: 0x04000027 RID: 39
            MV_E_NOENOUGH_BUF,
            // Token: 0x04000028 RID: 40
            MV_E_ABNORMAL_IMAGE,
            // Token: 0x04000029 RID: 41
            MV_E_LOAD_LIBRARY,
            // Token: 0x0400002A RID: 42
            MV_E_NOOUTBUF,
            // Token: 0x0400002B RID: 43
            MV_E_UNKNOW = -2147483393,
            // Token: 0x0400002C RID: 44
            MV_E_GC_GENERIC,
            // Token: 0x0400002D RID: 45
            MV_E_GC_ARGUMENT,
            // Token: 0x0400002E RID: 46
            MV_E_GC_RANGE,
            // Token: 0x0400002F RID: 47
            MV_E_GC_PROPERTY,
            // Token: 0x04000030 RID: 48
            MV_E_GC_RUNTIME,
            // Token: 0x04000031 RID: 49
            MV_E_GC_LOGICAL,
            // Token: 0x04000032 RID: 50
            MV_E_GC_ACCESS,
            // Token: 0x04000033 RID: 51
            MV_E_GC_TIMEOUT,
            // Token: 0x04000034 RID: 52
            MV_E_GC_DYNAMICCAST,
            // Token: 0x04000035 RID: 53
            MV_E_GC_UNKNOW = -2147483137,
            // Token: 0x04000036 RID: 54
            MV_E_NOT_IMPLEMENTED,
            // Token: 0x04000037 RID: 55
            MV_E_INVALID_ADDRESS,
            // Token: 0x04000038 RID: 56
            MV_E_WRITE_PROTECT,
            // Token: 0x04000039 RID: 57
            MV_E_ACCESS_DENIED,
            // Token: 0x0400003A RID: 58
            MV_E_BUSY,
            // Token: 0x0400003B RID: 59
            MV_E_PACKET,
            // Token: 0x0400003C RID: 60
            MV_E_NETER,
            // Token: 0x0400003D RID: 61
            MV_E_IP_CONFLICT = -2147483103,
            // Token: 0x0400003E RID: 62
            MV_E_USB_READ = -2147482880,
            // Token: 0x0400003F RID: 63
            MV_E_USB_WRITE,
            // Token: 0x04000040 RID: 64
            MV_E_USB_DEVICE,
            // Token: 0x04000041 RID: 65
            MV_E_USB_GENICAM,
            // Token: 0x04000042 RID: 66
            MV_E_USB_BANDWIDTH,
            // Token: 0x04000043 RID: 67
            MV_E_USB_DRIVER,
            // Token: 0x04000044 RID: 68
            MV_E_USB_UNKNOW = -2147482625,
            // Token: 0x04000045 RID: 69
            MV_E_UPG_FILE_MISMATCH,
            // Token: 0x04000046 RID: 70
            MV_E_UPG_LANGUSGE_MISMATCH,
            // Token: 0x04000047 RID: 71
            MV_E_UPG_CONFLICT,
            // Token: 0x04000048 RID: 72
            MV_E_UPG_INNER_ERR,
            // Token: 0x04000049 RID: 73
            MV_E_UPG_UNKNOW = -2147482369
        }
        #endregion
    }
}
