using FrameWork.Device;

namespace DemoMotionCard
{
    /// <summary>
    /// 模拟运动控制卡设备 - 用于测试和演示设备插件系统
    /// </summary>
    [DevicePlugin(DeviceType.MotionCard, "Demo Motion Card", Version = "1.0.0")]
    public class DemoMotionCard : MotionCardBase
    {
        #region 字段

        private readonly double[] _axisPositions;
        private readonly bool[] _axisMoving;
        private readonly bool[] _axisEnabled;
        private readonly double[] _axisVelocity;
        private readonly double[] _axisAcceleration;
        private readonly int _axisCount;
        private readonly Random _random = new();

        #endregion

        #region 构造函数

        public DemoMotionCard()
        {
            _axisCount = 4;
            _axisPositions = new double[_axisCount];
            _axisMoving = new bool[_axisCount];
            _axisEnabled = new bool[_axisCount];
            _axisVelocity = new double[_axisCount];
            _axisAcceleration = new double[_axisCount];

            // 初始化轴速度/加速度
            for (int i = 0; i < _axisCount; i++)
            {
                _axisVelocity[i] = 1000.0;
                _axisAcceleration[i] = 500.0;
            }

            DeviceCode = "MOT-DEMO";
            DeviceName = "Demo Motion Card";
            MotionInfo = new MotionCardInfo
            {
                AxisCount = _axisCount,
                ControlMode = "Simulated Pulse",
                MaxPulseFrequency = 1_000_000,
                SupportsLinearInterpolation = true,
                SupportsCircularInterpolation = true,
                SupportsContinuousMotion = true,
                InterfaceType = "Simulated"
            };
            Info = new DeviceInfo
            {
                DeviceCode = DeviceCode,
                DeviceName = DeviceName,
                DeviceType = DeviceType.MotionCard,
                Manufacturer = "Demo Corp",
                Model = "SIM-MOT-04",
                Version = "1.0.0",
                SerialNumber = "DEMO-00002",
                Description = "模拟4轴运动控制卡，用于测试设备加载框架"
            };
        }

        #endregion

        #region DeviceBase 覆写

        protected override Task<bool> OnConnectAsync()
        {
            return Task.FromResult(true);
        }

        protected override Task<bool> OnDisconnectAsync()
        {
            // 紧急停止所有轴
            for (int i = 0; i < _axisCount; i++)
            {
                _axisMoving[i] = false;
                _axisEnabled[i] = false;
                _axisPositions[i] = 0;
            }
            return Task.FromResult(true);
        }

        public override Task<DeviceState> GetStateAsync()
        {
            return Task.FromResult(State);
        }

        #endregion

        #region IMotionCard 实现

        public override Task<int> GetAxisCountAsync()
        {
            return Task.FromResult(_axisCount);
        }

        public override async Task MoveAbsoluteAsync(int axisIndex, double position, double velocity)
        {
            await ValidateAxisIndexAsync(axisIndex);
            EnsureAxisEnabled(axisIndex);

            _axisMoving[axisIndex] = true;
            _axisVelocity[axisIndex] = velocity;

            // 模拟运动时间（距离/速度）
            var distance = Math.Abs(position - _axisPositions[axisIndex]);
            var moveTimeMs = (int)(distance / velocity * 1000);
            moveTimeMs = Math.Max(100, Math.Min(moveTimeMs, 5000)); // 100ms~5s

            await Task.Delay(moveTimeMs);

            _axisPositions[axisIndex] = position;
            _axisMoving[axisIndex] = false;

            OnMotionCompleted(new AxisMotionCompletedEventArgs(
                DeviceCode, axisIndex, position, true));
        }

        public override async Task MoveRelativeAsync(int axisIndex, double distance, double velocity)
        {
            var targetPos = _axisPositions[axisIndex] + distance;
            await MoveAbsoluteAsync(axisIndex, targetPos, velocity);
        }

        public override async Task JogPositiveAsync(int axisIndex, double velocity)
        {
            await ValidateAxisIndexAsync(axisIndex);
            EnsureAxisEnabled(axisIndex);
            // JOG 模式下持续移动，这里简化为相对移动一小段
            await MoveRelativeAsync(axisIndex, 100, velocity);
        }

        public override async Task JogNegativeAsync(int axisIndex, double velocity)
        {
            await ValidateAxisIndexAsync(axisIndex);
            EnsureAxisEnabled(axisIndex);
            await MoveRelativeAsync(axisIndex, -100, velocity);
        }

        public override Task StopAxisAsync(int axisIndex)
        {
            if (axisIndex >= 0 && axisIndex < _axisCount)
            {
                _axisMoving[axisIndex] = false;
            }
            return Task.CompletedTask;
        }

        public override Task EmergencyStopAsync()
        {
            for (int i = 0; i < _axisCount; i++)
            {
                _axisMoving[i] = false;
            }
            return Task.CompletedTask;
        }

        public override async Task HomeAsync(int axisIndex)
        {
            await ValidateAxisIndexAsync(axisIndex);
            // 模拟回零：移动到0位
            await MoveAbsoluteAsync(axisIndex, 0, 500);
        }

        public override Task<double> GetAxisPositionAsync(int axisIndex)
        {
            if (axisIndex < 0 || axisIndex >= _axisCount)
                throw new ArgumentOutOfRangeException(nameof(axisIndex));
            return Task.FromResult(_axisPositions[axisIndex]);
        }

        public override Task<bool> IsAxisMovingAsync(int axisIndex)
        {
            if (axisIndex < 0 || axisIndex >= _axisCount)
                throw new ArgumentOutOfRangeException(nameof(axisIndex));
            return Task.FromResult(_axisMoving[axisIndex]);
        }

        public override Task SetAxisVelocityAsync(int axisIndex, double velocity)
        {
            if (axisIndex < 0 || axisIndex >= _axisCount)
                throw new ArgumentOutOfRangeException(nameof(axisIndex));
            _axisVelocity[axisIndex] = velocity;
            return Task.CompletedTask;
        }

        public override Task SetAxisAccelerationAsync(int axisIndex, double acceleration)
        {
            if (axisIndex < 0 || axisIndex >= _axisCount)
                throw new ArgumentOutOfRangeException(nameof(axisIndex));
            _axisAcceleration[axisIndex] = acceleration;
            return Task.CompletedTask;
        }

        public override Task SetAxisEnabledAsync(int axisIndex, bool enabled)
        {
            if (axisIndex < 0 || axisIndex >= _axisCount)
                throw new ArgumentOutOfRangeException(nameof(axisIndex));
            _axisEnabled[axisIndex] = enabled;
            return Task.CompletedTask;
        }

        #endregion

        #region 私有方法

        private void EnsureAxisEnabled(int axisIndex)
        {
            if (!_axisEnabled[axisIndex])
            {
                // 自动使能轴（模拟场景）
                _axisEnabled[axisIndex] = true;
            }
        }

        #endregion
    }
}
