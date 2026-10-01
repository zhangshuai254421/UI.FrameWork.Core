using Framework.Device.Domain;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 假相机适配器：实现 <see cref="ICamera"/> 契约，用于契约测试，不接触任何真实 SDK。
    /// </summary>
    public class FakeCamera : CameraBase
    {
        /// <summary>记录最近一次设置的曝光时间（微秒），用于验证读写一致。</summary>
        private double _exposureTimeUs;
        /// <summary>记录最近一次设置的增益（dB），用于验证读写一致。</summary>
        private double _gain;
        /// <summary>下一次取帧返回的图像，由 SetNextFrame 预先注入。</summary>
        private CameraData _frame = new();

        /// <summary>最近一次 Open 收到的连接参数，供测试断言透传是否正确。</summary>
        public DeviceConnection? OpenedConnection { get; private set; }

        /// <summary>相机是否已打开。</summary>
        public bool IsOpen { get; private set; }

        /// <summary>相机是否正在取流。</summary>
        public bool IsAcquiring { get; private set; }

        /// <summary>软触发被调用的次数，供测试断言。</summary>
        public int TriggerCount { get; private set; }

        /// <inheritdoc />
        public override string Name => "FakeCamera";

        /// <summary>预置下一帧图像，使随后的 GetOneImage 返回该帧。</summary>
        public void SetNextFrame(CameraData frame) => _frame = frame;

        /// <inheritdoc />
        public override bool Open(DeviceConnection connection)
        {
            OpenedConnection = connection;
            IsOpen = true;
            return true;
        }

        /// <inheritdoc />
        public override void Close()
        {
            IsOpen = false;
            IsAcquiring = false;
        }

        /// <inheritdoc />
        public override void StartAcquisition() => IsAcquiring = true;

        /// <inheritdoc />
        public override void StopAcquisition() => IsAcquiring = false;

        /// <inheritdoc />
        public override CameraData GetOneImage() => _frame;

        /// <inheritdoc />
        public override void SetExposureTime(double exposureTimeUs) => _exposureTimeUs = exposureTimeUs;

        /// <inheritdoc />
        public override double GetExposureTime() => _exposureTimeUs;

        /// <inheritdoc />
        public override void SetGain(double gain) => _gain = gain;

        /// <inheritdoc />
        public override double GetGain() => _gain;

        /// <inheritdoc />
        public override void TriggerSoftware() => TriggerCount++;
    }
}
