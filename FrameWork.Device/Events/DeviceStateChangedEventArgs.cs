namespace FrameWork.Device
{
    /// <summary>
    /// 设备状态变更事件参数
    /// </summary>
    public class DeviceStateChangedEventArgs : EventArgs
    {
        /// <summary>设备编码</summary>
        public string DeviceCode { get; }

        /// <summary>变更前的状态</summary>
        public DeviceState OldState { get; }

        /// <summary>变更后的状态</summary>
        public DeviceState NewState { get; }

        /// <summary>错误信息（如有）</summary>
        public string? ErrorMessage { get; }

        /// <summary>发生时间</summary>
        public DateTime Timestamp { get; }

        public DeviceStateChangedEventArgs(
            string deviceCode,
            DeviceState oldState,
            DeviceState newState,
            string? errorMessage = null)
        {
            DeviceCode = deviceCode;
            OldState = oldState;
            NewState = newState;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }
    }
}
