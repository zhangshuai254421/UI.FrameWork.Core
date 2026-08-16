namespace FrameWork.Device
{
    /// <summary>
    /// 运动控制卡接口 - 完整操作契约
    /// </summary>
    public interface IMotionCard : IDevice
    {
        /// <summary>
        /// 运动控制卡能力信息
        /// </summary>
        MotionCardInfo MotionInfo { get; }

        /// <summary>
        /// 获取卡支持的轴数
        /// </summary>
        Task<int> GetAxisCountAsync();

        /// <summary>
        /// 绝对位置移动
        /// </summary>
        /// <param name="axisIndex">轴索引（从0开始）</param>
        /// <param name="position">目标位置（脉冲或工程单位）</param>
        /// <param name="velocity">运行速度</param>
        Task MoveAbsoluteAsync(int axisIndex, double position, double velocity);

        /// <summary>
        /// 相对位置移动
        /// </summary>
        /// <param name="axisIndex">轴索引（从0开始）</param>
        /// <param name="distance">移动距离</param>
        /// <param name="velocity">运行速度</param>
        Task MoveRelativeAsync(int axisIndex, double distance, double velocity);

        /// <summary>
        /// JOG（点动）正向移动
        /// </summary>
        Task JogPositiveAsync(int axisIndex, double velocity);

        /// <summary>
        /// JOG（点动）负向移动
        /// </summary>
        Task JogNegativeAsync(int axisIndex, double velocity);

        /// <summary>
        /// 停止指定轴
        /// </summary>
        Task StopAxisAsync(int axisIndex);

        /// <summary>
        /// 紧急停止所有轴
        /// </summary>
        Task EmergencyStopAsync();

        /// <summary>
        /// 回零（归零）操作
        /// </summary>
        Task HomeAsync(int axisIndex);

        /// <summary>
        /// 获取轴当前位置
        /// </summary>
        /// <returns>当前位置值</returns>
        Task<double> GetAxisPositionAsync(int axisIndex);

        /// <summary>
        /// 获取轴是否正在运动
        /// </summary>
        Task<bool> IsAxisMovingAsync(int axisIndex);

        /// <summary>
        /// 设置轴速度
        /// </summary>
        Task SetAxisVelocityAsync(int axisIndex, double velocity);

        /// <summary>
        /// 设置轴加速度
        /// </summary>
        Task SetAxisAccelerationAsync(int axisIndex, double acceleration);

        /// <summary>
        /// 使能/去使能指定轴
        /// </summary>
        Task SetAxisEnabledAsync(int axisIndex, bool enabled);

        /// <summary>
        /// 轴运动完成事件
        /// </summary>
        event EventHandler<AxisMotionCompletedEventArgs>? MotionCompleted;
    }

    /// <summary>
    /// 运动控制卡能力/参数信息
    /// </summary>
    public class MotionCardInfo
    {
        /// <summary>支持的轴数</summary>
        public int AxisCount { get; set; }

        /// <summary>控制方式（脉冲, EtherCAT, CANopen 等）</summary>
        public string ControlMode { get; set; } = string.Empty;

        /// <summary>最大脉冲频率（Hz）</summary>
        public double MaxPulseFrequency { get; set; }

        /// <summary>是否支持线性插补</summary>
        public bool SupportsLinearInterpolation { get; set; }

        /// <summary>是否支持圆弧插补</summary>
        public bool SupportsCircularInterpolation { get; set; }

        /// <summary>是否支持连续轨迹</summary>
        public bool SupportsContinuousMotion { get; set; }

        /// <summary>接口类型（PCI, PCIe, EtherCAT 等）</summary>
        public string InterfaceType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 轴运动完成事件参数
    /// </summary>
    public class AxisMotionCompletedEventArgs : EventArgs
    {
        /// <summary>设备编码</summary>
        public string DeviceCode { get; }

        /// <summary>轴索引</summary>
        public int AxisIndex { get; }

        /// <summary>完成时的位置</summary>
        public double FinalPosition { get; }

        /// <summary>是否正常完成</summary>
        public bool IsNormalCompletion { get; }

        /// <summary>错误信息（如有）</summary>
        public string? ErrorMessage { get; }

        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; }

        public AxisMotionCompletedEventArgs(
            string deviceCode,
            int axisIndex,
            double finalPosition,
            bool isNormalCompletion,
            string? errorMessage = null)
        {
            DeviceCode = deviceCode;
            AxisIndex = axisIndex;
            FinalPosition = finalPosition;
            IsNormalCompletion = isNormalCompletion;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.Now;
        }
    }
}
