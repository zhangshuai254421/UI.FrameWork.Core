using Framework.Device;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MvCameraControl;
using SdkDevice = MvCameraControl.IDevice;

namespace Framework.Device.HikVision
{
    /// <summary>
    /// 海康相机适配器：把强类型相机契约翻译成海康 SDK 的私有调用。
    /// 图像以托管字节数组返回，SDK 对象与裸指针不外泄；SDK 错误码翻译成统一错误类别供上层判读。
    /// </summary>
    [DeviceAdapter(DeviceKind.Camera, "HikVision")]
    public class CameraHikVision : CameraBase
    {
        /// <summary>单帧取流超时（毫秒）。</summary>
        private const uint GrabTimeoutMs = 1000;

        private static readonly object SdkInitLock = new();
        private static bool _sdkInitialized;

        private readonly ILogger<CameraHikVision> _logger;

        /// <summary>当前打开的 SDK 设备；未打开为 null。各方法先把它拷贝到局部变量再用，避免执行中途被 <see cref="Close"/> 置空。</summary>
        private SdkDevice? _camera;

        /// <summary>无参构造：供适配器工厂按「(设备类型, 厂商)」反射实例化（不注入日志）。</summary>
        public CameraHikVision() : this(null)
        {
        }

        public CameraHikVision(ILogger<CameraHikVision>? logger)
        {
            _logger = logger ?? NullLogger<CameraHikVision>.Instance;
        }

        /// <summary>适配器名。</summary>
        public override string Name => "HikVisionCamera";

        /// <summary>最近一次 SDK 调用失败翻译出的统一错误类别。</summary>
        public DeviceErrorCategory LastError { get; private set; } = DeviceErrorCategory.None;

        /// <summary>按 TCP 连接中的 IP 定位并打开相机；重复打开先释放旧设备。失败原因见 <see cref="LastError"/>。</summary>
        public override bool Open(DeviceConnection connection)
        {
            if (!EnsureSdkInitialized())
            {
                LastError = DeviceErrorCategory.OpenFailed;
                _logger.LogError("海康 SDK 初始化失败，无法打开相机");
                return false;
            }

            if (connection is not TcpConnection tcp)
            {
                LastError = DeviceErrorCategory.ParameterNotSupported;
                _logger.LogError("海康相机适配器仅支持 TCP 连接，收到 {Type}", connection.GetType().Name);
                return false;
            }

            // 若已打开，先释放旧设备，避免重复打开泄漏句柄。
            ReleaseCamera();

            _camera = CreateCameraByIp(tcp.IpAddress);
            if (_camera is null)
            {
                return false;
            }

            int ret = _camera.Open();
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(Open));
                ReleaseCamera();
                return false;
            }

            LastError = DeviceErrorCategory.None;
            return true;
        }

        /// <summary>停止取流并释放设备；未打开时为空操作，可重复调用。</summary>
        public override void Close()
        {
            ReleaseCamera();
        }

        /// <summary>开始取流；未打开时记录错误并忽略。</summary>
        public override void StartAcquisition()
        {
            var camera = _camera;
            if (camera is null)
            {
                RecordNotOpen(nameof(StartAcquisition));
                return;
            }

            int ret = camera.StreamGrabber.StartGrabbing();
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(StartAcquisition));
            }
        }

        /// <summary>停止取流；未打开时静默忽略。</summary>
        public override void StopAcquisition()
        {
            var camera = _camera;
            if (camera is null)
            {
                return;
            }

            int ret = camera.StreamGrabber.StopGrabbing();
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(StopAcquisition));
            }
        }

        /// <summary>阻塞取一帧（超时 <see cref="GrabTimeoutMs"/> 毫秒）；未打开、超时或失败返回空 <see cref="CameraData"/>，SDK 帧缓冲在本方法内归还。</summary>
        public override CameraData GetOneImage()
        {
            var camera = _camera;
            if (camera is null)
            {
                RecordNotOpen(nameof(GetOneImage));
                return new CameraData();
            }

            int ret = camera.StreamGrabber.GetImageBuffer(GrabTimeoutMs, out IFrameOut? frame);
            if (ret != MvError.MV_OK || frame is null || frame.Image is null)
            {
                RecordError(ret, nameof(GetOneImage));
                return new CameraData();
            }

            try
            {
                var image = frame.Image;
                return new CameraData
                {
                    ImageData = image.PixelData ?? Array.Empty<byte>(),
                    Width = (int)image.Width,
                    Height = (int)image.Height,
                    PixelFormat = TranslatePixelFormat((int)image.PixelType),
                };
            }
            finally
            {
                camera.StreamGrabber.FreeImageBuffer(frame);
            }
        }

        /// <summary>设置曝光时间（微秒），写入 SDK 的 ExposureTime 节点。</summary>
        public override void SetExposureTime(double exposureTimeUs)
        {
            var camera = _camera;
            if (camera is null)
            {
                RecordNotOpen(nameof(SetExposureTime));
                return;
            }

            int ret = camera.Parameters.SetFloatValue("ExposureTime", (float)exposureTimeUs);
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(SetExposureTime));
            }
        }

        /// <summary>读取曝光时间（微秒）；未打开或读取失败返回 0。</summary>
        public override double GetExposureTime()
        {
            var camera = _camera;
            if (camera is null)
            {
                RecordNotOpen(nameof(GetExposureTime));
                return 0;
            }

            int ret = camera.Parameters.GetFloatValue("ExposureTime", out IFloatValue? value);
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(GetExposureTime));
                return 0;
            }

            return value?.CurValue ?? 0;
        }

        /// <summary>设置增益（dB），写入 SDK 的 Gain 节点。</summary>
        public override void SetGain(double gain)
        {
            var camera = _camera;
            if (camera is null)
            {
                RecordNotOpen(nameof(SetGain));
                return;
            }

            int ret = camera.Parameters.SetFloatValue("Gain", (float)gain);
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(SetGain));
            }
        }

        /// <summary>读取增益（dB）；未打开或读取失败返回 0。</summary>
        public override double GetGain()
        {
            var camera = _camera;
            if (camera is null)
            {
                RecordNotOpen(nameof(GetGain));
                return 0;
            }

            int ret = camera.Parameters.GetFloatValue("Gain", out IFloatValue? value);
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(GetGain));
                return 0;
            }

            return value?.CurValue ?? 0;
        }

        /// <summary>执行一次软触发；若相机未处于软触发模式，SDK 返回错误并记录。</summary>
        public override void TriggerSoftware()
        {
            var camera = _camera;
            if (camera is null)
            {
                RecordNotOpen(nameof(TriggerSoftware));
                return;
            }

            int ret = camera.Parameters.SetCommandValue("TriggerSoftware");
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(TriggerSoftware));
            }
        }

        /// <summary>把 SDK 错误码翻译成统一错误类别（纯函数，供上层判读）。</summary>
        public static DeviceErrorCategory TranslateError(int sdkErrorCode)
        {
            switch (sdkErrorCode)
            {
                case MvError.MV_OK:
                    return DeviceErrorCategory.None;

                // 参数不支持 / 节点不存在 / 未实现
                case MvError.MV_E_SUPPORT:
                case MvError.MV_E_PARAMETER:
                case MvError.MV_E_PARAMETER_RANGE:
                case MvError.MV_E_GC_NODE_NOT_FOUND:
                case MvError.MV_E_NOT_IMPLEMENTED:
                case MvError.MV_E_WRITE_PROTECT:
                    return DeviceErrorCategory.ParameterNotSupported;

                // 找不到设备 / 无响应 / 断连 / 网络错误
                case MvError.MV_E_NORESPONSE:
                case MvError.MV_E_NETER:
                case MvError.MV_E_DEV_DISCONNECT:
                    return DeviceErrorCategory.DeviceNotFound;

                // 打开失败 / 句柄无效 / 资源 / 访问拒绝 / 忙
                case MvError.MV_E_HANDLE:
                case MvError.MV_E_RESOURCE:
                case MvError.MV_E_ACCESS_DENIED:
                case MvError.MV_E_BUSY:
                    return DeviceErrorCategory.OpenFailed;

                default:
                    return DeviceErrorCategory.Unknown;
            }
        }

        /// <summary>
        /// 把海康 GVSP 像素格式翻译成品牌无关像素格式（纯函数）。
        /// 契约未定义的打包格式（如 Mono10_Packed）一律返回 Undefined。
        /// </summary>
        public static PixelFormat TranslatePixelFormat(int gvspPixelType)
        {
            switch (gvspPixelType)
            {
                case (int)MvGvspPixelType.PixelType_Gvsp_Mono8:
                    return PixelFormat.Mono8;
                case (int)MvGvspPixelType.PixelType_Gvsp_Mono10:
                    return PixelFormat.Mono10;
                case (int)MvGvspPixelType.PixelType_Gvsp_Mono12:
                    return PixelFormat.Mono12;
                case (int)MvGvspPixelType.PixelType_Gvsp_Mono16:
                    return PixelFormat.Mono16;
                case (int)MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                    return PixelFormat.BayerGR8;
                case (int)MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                    return PixelFormat.BayerRG8;
                case (int)MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                    return PixelFormat.BayerGB8;
                case (int)MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                    return PixelFormat.BayerBG8;
                case (int)MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                    return PixelFormat.RGB8;
                case (int)MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                    return PixelFormat.BGR8;
                default:
                    return PixelFormat.Undefined;
            }
        }

        /// <summary>枚举 GigE/USB 设备并按 IP 匹配，创建 SDK 设备实例；枚举失败或找不到时返回 null 并置 <see cref="LastError"/>。</summary>
        private SdkDevice? CreateCameraByIp(string ipAddress)
        {
            List<IDeviceInfo> devices = new();
            int ret = DeviceEnumerator.EnumDevices(
                DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice,
                out devices);
            if (ret != MvError.MV_OK)
            {
                RecordError(ret, nameof(DeviceEnumerator.EnumDevices));
                return null;
            }

            // TCP 连接按 IP 匹配，仅对 GigE 设备生效；USB 设备无 IP 概念，不会命中。
            foreach (var info in devices)
            {
                if (info is IGigEDeviceInfo gige && FormatIp(gige.CurrentIp) == ipAddress)
                {
                    return DeviceFactory.CreateDevice(info);
                }
            }

            LastError = DeviceErrorCategory.DeviceNotFound;
            _logger.LogError("未找到 IP 为 {Ip} 的海康相机", ipAddress);
            return null;
        }

        /// <summary>把 SDK 返回的大端序 uint IP 转成点分十进制字符串。</summary>
        private static string FormatIp(uint ip) =>
            $"{(ip >> 24) & 0xff}.{(ip >> 16) & 0xff}.{(ip >> 8) & 0xff}.{ip & 0xff}";

        /// <summary>进程内一次性初始化 SDK（双检锁）；仅成功才置位，失败由下次 Open 重试。</summary>
        private static bool EnsureSdkInitialized()
        {
            if (_sdkInitialized)
            {
                return true;
            }

            lock (SdkInitLock)
            {
                if (_sdkInitialized)
                {
                    return true;
                }

                // 仅初始化成功才置位；失败则下次 Open 时重试。
                _sdkInitialized = SDKSystem.Initialize() == MvError.MV_OK;
                return _sdkInitialized;
            }
        }

        /// <summary>按「停流 → 关闭 → 释放」顺序清理设备；未打开为空操作。Open 失败与 Close 共用。</summary>
        private void ReleaseCamera()
        {
            var camera = _camera;
            _camera = null;
            if (camera is null)
            {
                return;
            }

            camera.StreamGrabber.StopGrabbing();
            camera.Close();
            camera.Dispose();
        }

        /// <summary>统一错误出口：翻译 SDK 错误码到 <see cref="LastError"/> 并写日志。</summary>
        private void RecordError(int sdkErrorCode, string operation)
        {
            LastError = TranslateError(sdkErrorCode);
            _logger.LogError(
                "海康相机 {Operation} 失败，错误码 {Code:X8}，类别 {Category}",
                operation, sdkErrorCode, LastError);
        }

        /// <summary>未打开设备就调用的错误出口：标记 <see cref="LastError"/> 并写日志。</summary>
        private void RecordNotOpen(string operation)
        {
            LastError = DeviceErrorCategory.OpenFailed;
            _logger.LogError("海康相机未打开，无法执行 {Operation}", operation);
        }
    }
}
