namespace FrameWork.Device
{
    /// <summary>
    /// 相机抽象基类 - 提供相机通用状态管理和默认实现骨架
    /// 第三方相机插件继承此类，覆写品牌特定的操作方法
    /// </summary>
    public abstract class CameraBase : DeviceBase, ICamera
    {
        #region 属性

        public override DeviceType DeviceType => DeviceType.Camera;

        public CameraInfo CameraInfo { get; protected set; } = new();

        #endregion

        #region 事件

        public event EventHandler<ImageAcquiredEventArgs>? ImageAcquired;

        #endregion

        #region ICamera 抽象方法（子类必须实现）

        public abstract Task<ImageAcquiredEventArgs> SnapAsync();
        public abstract Task StartContinuousGrabAsync();
        public abstract Task StopContinuousGrabAsync();
        public abstract Task SetExposureAsync(double exposureUs);
        public abstract Task<double> GetExposureAsync();
        public abstract Task SetGainAsync(double gain);
        public abstract Task<double> GetGainAsync();
        public abstract Task SetTriggerModeAsync(bool isTriggerMode);
        public abstract Task SoftTriggerAsync();
        public abstract override Task<DeviceState> GetStateAsync();

        #endregion

        #region 受保护方法

        /// <summary>
        /// 触发图像采集事件（子类在收到图像数据时调用）
        /// </summary>
        protected virtual void OnImageAcquired(ImageAcquiredEventArgs e)
        {
            ImageAcquired?.Invoke(this, e);
        }

        #endregion

        #region DeviceBase 抽象方法

        protected abstract override Task<bool> OnConnectAsync();
        protected abstract override Task<bool> OnDisconnectAsync();

        #endregion
    }
}
