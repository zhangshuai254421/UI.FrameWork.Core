namespace Device.Domain
{
    /// <summary>
    /// 设备管理器接口 - 管理所有硬件设备的生命周期
    /// </summary>
    public interface IDeviceManager
    {
        /// <summary>
        /// 从数据库加载所有启用的设备配置，加载插件 DLL，创建并连接所有设备
        /// </summary>
        Task InitializeAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 断开并释放所有设备
        /// </summary>
        Task ShutdownAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据设备编码获取设备实例
        /// </summary>
        T? GetDevice<T>(string deviceCode) where T : class, FrameWork.Device.IDevice;

        /// <summary>
        /// 获取所有指定类型的设备
        /// </summary>
        IReadOnlyList<T> GetAllDevices<T>() where T : class, FrameWork.Device.IDevice;

        /// <summary>
        /// 获取所有已加载的相机设备
        /// </summary>
        IReadOnlyList<FrameWork.Device.ICamera> GetCameras();

        /// <summary>
        /// 获取所有已加载的运动控制卡设备
        /// </summary>
        IReadOnlyList<FrameWork.Device.IMotionCard> GetMotionCards();

        /// <summary>
        /// 获取所有已加载的设备
        /// </summary>
        IReadOnlyList<FrameWork.Device.IDevice> GetAllDevices();

        /// <summary>
        /// 设备是否已初始化
        /// </summary>
        bool IsInitialized { get; }
    }
}
