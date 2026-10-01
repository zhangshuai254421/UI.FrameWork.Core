namespace Framework.Device.Domain
{
    /// <summary>
    /// 设备类型：设备所属的类别，决定它具备哪套能力。
    /// </summary>
    public enum DeviceKind
    {
        /// <summary>相机。</summary>
        Camera = 1,

        /// <summary>运动控制卡。</summary>
        MotionControlCard = 2,

        /// <summary>其他设备。</summary>
        Other = 3,
    }
}
