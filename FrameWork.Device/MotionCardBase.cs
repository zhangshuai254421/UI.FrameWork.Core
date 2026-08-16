namespace FrameWork.Device
{
    /// <summary>
    /// 运动控制卡抽象基类 - 提供通用轴管理和默认实现骨架
    /// 第三方运动控制卡插件继承此类，覆写品牌特定的操作方法
    /// </summary>
    public abstract class MotionCardBase : DeviceBase, IMotionCard
    {
        #region 属性

        public override DeviceType DeviceType => DeviceType.MotionCard;

        public MotionCardInfo MotionInfo { get; protected set; } = new();

        #endregion

        #region 事件

        public event EventHandler<AxisMotionCompletedEventArgs>? MotionCompleted;

        #endregion

        #region IMotionCard 抽象方法（子类必须实现）

        public abstract Task<int> GetAxisCountAsync();
        public abstract Task MoveAbsoluteAsync(int axisIndex, double position, double velocity);
        public abstract Task MoveRelativeAsync(int axisIndex, double distance, double velocity);
        public abstract Task JogPositiveAsync(int axisIndex, double velocity);
        public abstract Task JogNegativeAsync(int axisIndex, double velocity);
        public abstract Task StopAxisAsync(int axisIndex);
        public abstract Task EmergencyStopAsync();
        public abstract Task HomeAsync(int axisIndex);
        public abstract Task<double> GetAxisPositionAsync(int axisIndex);
        public abstract Task<bool> IsAxisMovingAsync(int axisIndex);
        public abstract Task SetAxisVelocityAsync(int axisIndex, double velocity);
        public abstract Task SetAxisAccelerationAsync(int axisIndex, double acceleration);
        public abstract Task SetAxisEnabledAsync(int axisIndex, bool enabled);
        public abstract override Task<DeviceState> GetStateAsync();

        #endregion

        #region 受保护方法

        /// <summary>
        /// 触发轴运动完成事件（子类在轴运动完成时调用）
        /// </summary>
        protected virtual void OnMotionCompleted(AxisMotionCompletedEventArgs e)
        {
            MotionCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// 验证轴索引是否在有效范围内
        /// </summary>
        protected virtual async Task ValidateAxisIndexAsync(int axisIndex)
        {
            var count = await GetAxisCountAsync();
            if (axisIndex < 0 || axisIndex >= count)
                throw new ArgumentOutOfRangeException(
                    nameof(axisIndex),
                    $"轴索引 {axisIndex} 超出范围 [0, {count - 1}]");
        }

        #endregion

        #region DeviceBase 抽象方法

        protected abstract override Task<bool> OnConnectAsync();
        protected abstract override Task<bool> OnDisconnectAsync();

        #endregion
    }
}
