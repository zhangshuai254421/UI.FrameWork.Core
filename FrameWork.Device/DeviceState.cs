namespace FrameWork.Device
{
    /// <summary>
    /// 设备状态枚举
    /// </summary>
    public enum DeviceState
    {
        /// <summary>未初始化</summary>
        Uninitialized = 0,

        /// <summary>已断开连接</summary>
        Disconnected = 1,

        /// <summary>正在连接中</summary>
        Connecting = 2,

        /// <summary>已连接，正常运行</summary>
        Connected = 3,

        /// <summary>正在断开连接</summary>
        Disconnecting = 4,

        /// <summary>错误状态</summary>
        Error = 5
    }
}
