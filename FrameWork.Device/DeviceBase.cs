namespace FrameWork.Device
{
    /// <summary>
    /// 设备抽象基类 - 提供通用状态管理和 IDisposable 实现
    /// 第三方硬件插件继承此类，覆写品牌特定的连接/断开逻辑
    /// </summary>
    public abstract class DeviceBase : IDevice
    {
        #region 字段

        private DeviceState _state = DeviceState.Uninitialized;
        private readonly object _stateLock = new();
        private bool _disposed;

        #endregion

        #region 属性

        public string DeviceCode { get; protected set; } = string.Empty;
        public string DeviceName { get; protected set; } = string.Empty;
        public abstract DeviceType DeviceType { get; }
        public DeviceInfo Info { get; protected set; } = new();

        public DeviceState State
        {
            get { lock (_stateLock) return _state; }
            protected set => SetState(value);
        }

        #endregion

        #region 事件

        public event EventHandler<DeviceStateChangedEventArgs>? StateChanged;

        #endregion

        #region 公共方法

        public async Task<bool> ConnectAsync()
        {
            if (State == DeviceState.Connected)
                return true;

            SetState(DeviceState.Connecting);

            try
            {
                var result = await OnConnectAsync();
                if (result)
                {
                    SetState(DeviceState.Connected);
                }
                else
                {
                    SetState(DeviceState.Error, "连接返回失败");
                }
                return result;
            }
            catch (Exception ex)
            {
                SetState(DeviceState.Error, ex.Message);
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            if (State == DeviceState.Disconnected || State == DeviceState.Uninitialized)
                return true;

            SetState(DeviceState.Disconnecting);

            try
            {
                var result = await OnDisconnectAsync();
                SetState(result ? DeviceState.Disconnected : DeviceState.Error);
                return result;
            }
            catch (Exception ex)
            {
                SetState(DeviceState.Error, ex.Message);
                return false;
            }
        }

        public abstract Task<DeviceState> GetStateAsync();

        #endregion

        #region 虚方法（子类覆写）

        /// <summary>
        /// 执行实际的连接逻辑（子类覆写）
        /// </summary>
        protected abstract Task<bool> OnConnectAsync();

        /// <summary>
        /// 执行实际的断开逻辑（子类覆写）
        /// </summary>
        protected abstract Task<bool> OnDisconnectAsync();

        #endregion

        #region 状态管理

        /// <summary>
        /// 线程安全地设置状态并触发事件
        /// </summary>
        protected virtual void SetState(DeviceState newState, string? errorMessage = null)
        {
            DeviceState oldState;
            lock (_stateLock)
            {
                oldState = _state;
                _state = newState;
            }

            if (oldState != newState)
            {
                OnStateChanged(oldState, newState, errorMessage);
            }
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        protected virtual void OnStateChanged(DeviceState oldState, DeviceState newState, string? errorMessage)
        {
            StateChanged?.Invoke(this, new DeviceStateChangedEventArgs(
                DeviceCode, oldState, newState, errorMessage));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 确保断开连接
                if (State == DeviceState.Connected)
                {
                    try
                    {
                        DisconnectAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // 忽略 dispose 中的异常
                    }
                }
            }

            _disposed = true;
        }

        #endregion
    }
}
