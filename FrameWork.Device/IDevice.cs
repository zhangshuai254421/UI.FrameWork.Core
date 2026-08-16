namespace FrameWork.Device
{
    /// <summary>
    /// 设备基础接口 - 所有硬件设备的根契约
    /// </summary>
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// 设备唯一编码（对应 DeviceConfiguration.DeviceCode）
        /// </summary>
        string DeviceCode { get; }

        /// <summary>
        /// 设备显示名称
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// 设备类型
        /// </summary>
        DeviceType DeviceType { get; }

        /// <summary>
        /// 当前设备状态
        /// </summary>
        DeviceState State { get; }

        /// <summary>
        /// 设备信息（厂商、型号、版本等）
        /// </summary>
        DeviceInfo Info { get; }

        /// <summary>
        /// 异步连接到设备
        /// </summary>
        /// <returns>连接是否成功</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// 异步断开设备连接
        /// </summary>
        /// <returns>断开是否成功</returns>
        Task<bool> DisconnectAsync();

        /// <summary>
        /// 获取设备当前状态
        /// </summary>
        Task<DeviceState> GetStateAsync();

        /// <summary>
        /// 设备状态变更事件
        /// </summary>
        event EventHandler<DeviceStateChangedEventArgs> StateChanged;
    }
}
