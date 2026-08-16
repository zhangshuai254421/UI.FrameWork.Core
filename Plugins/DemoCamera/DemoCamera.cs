using FrameWork.Device;

namespace DemoCamera
{
    /// <summary>
    /// 模拟相机设备 - 用于测试和演示设备插件系统
    /// </summary>
    [DevicePlugin(DeviceType.Camera, "Demo Camera", Version = "1.0.0")]
    public class DemoCamera : CameraBase
    {
        #region 字段

        private double _exposureUs = 10000; // 10ms 默认曝光
        private double _gain = 1.0;
        private bool _isTriggerMode = false;
        private bool _isGrabbing;
        private CancellationTokenSource? _grabCts;
        private readonly Random _random = new();

        #endregion

        #region 构造函数

        public DemoCamera()
        {
            DeviceCode = "CAM-DEMO";
            DeviceName = "Demo Camera";
            CameraInfo = new CameraInfo
            {
                SensorWidth = 1920,
                SensorHeight = 1080,
                PixelBitDepth = 8,
                IsColor = true,
                MinExposureUs = 10,
                MaxExposureUs = 10000000,
                MinGain = 0,
                MaxGain = 24,
                MaxFrameRate = 30,
                InterfaceType = "Simulated"
            };
            Info = new DeviceInfo
            {
                DeviceCode = DeviceCode,
                DeviceName = DeviceName,
                DeviceType = DeviceType.Camera,
                Manufacturer = "Demo Corp",
                Model = "SIM-CAM-01",
                Version = "1.0.0",
                SerialNumber = "DEMO-00001",
                Description = "模拟工业相机，用于测试设备加载框架"
            };
        }

        #endregion

        #region DeviceBase 覆写

        protected override Task<bool> OnConnectAsync()
        {
            // 模拟连接延迟
            return Task.FromResult(true);
        }

        protected override Task<bool> OnDisconnectAsync()
        {
            StopGrabInternal();
            return Task.FromResult(true);
        }

        public override Task<DeviceState> GetStateAsync()
        {
            return Task.FromResult(State);
        }

        #endregion

        #region ICamera 实现

        public override async Task<ImageAcquiredEventArgs> SnapAsync()
        {
            await Task.Delay(50); // 模拟曝光时间

            var imageData = GenerateFakeImage();
            var args = new ImageAcquiredEventArgs(
                DeviceCode, 1920, 1080, "Mono8", imageData);

            OnImageAcquired(args);
            return args;
        }

        public override Task StartContinuousGrabAsync()
        {
            if (_isGrabbing) return Task.CompletedTask;

            _isGrabbing = true;
            _grabCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!_grabCts.Token.IsCancellationRequested)
                {
                    await SnapAsync();
                    await Task.Delay(33, _grabCts.Token); // ~30fps
                }
            }, _grabCts.Token);

            return Task.CompletedTask;
        }

        public override Task StopContinuousGrabAsync()
        {
            StopGrabInternal();
            return Task.CompletedTask;
        }

        public override Task SetExposureAsync(double exposureUs)
        {
            _exposureUs = Math.Clamp(exposureUs, CameraInfo.MinExposureUs, CameraInfo.MaxExposureUs);
            return Task.CompletedTask;
        }

        public override Task<double> GetExposureAsync()
        {
            return Task.FromResult(_exposureUs);
        }

        public override Task SetGainAsync(double gain)
        {
            _gain = Math.Clamp(gain, CameraInfo.MinGain, CameraInfo.MaxGain);
            return Task.CompletedTask;
        }

        public override Task<double> GetGainAsync()
        {
            return Task.FromResult(_gain);
        }

        public override Task SetTriggerModeAsync(bool isTriggerMode)
        {
            _isTriggerMode = isTriggerMode;
            return Task.CompletedTask;
        }

        public override Task SoftTriggerAsync()
        {
            return SnapAsync();
        }

        #endregion

        #region 私有方法

        private void StopGrabInternal()
        {
            _isGrabbing = false;
            _grabCts?.Cancel();
            _grabCts?.Dispose();
            _grabCts = null;
        }

        private byte[] GenerateFakeImage()
        {
            // 生成简单的渐变测试图像 (1920x1080, 8-bit Mono)
            var data = new byte[1920 * 1080];
            for (int y = 0; y < 1080; y++)
            {
                for (int x = 0; x < 1920; x++)
                {
                    // 对角线条纹 + 随机噪声
                    data[y * 1920 + x] = (byte)(((x + y) % 256 + _random.Next(0, 20)) & 0xFF);
                }
            }
            return data;
        }

        #endregion
    }
}
