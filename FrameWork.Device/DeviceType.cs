namespace FrameWork.Device
{
    /// <summary>
    /// 设备类型枚举
    /// </summary>
    public enum DeviceType
    {
        /// <summary>工业相机</summary>
        Camera = 0,

        /// <summary>运动控制卡</summary>
        MotionCard = 1,

        /// <summary>PLC 控制器</summary>
        PLC = 2,

        /// <summary>传感器/采集器</summary>
        Sensor = 3,

        /// <summary>其他设备</summary>
        Other = 99
    }
}
