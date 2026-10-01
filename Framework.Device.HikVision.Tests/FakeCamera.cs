using Framework.Device.Domain;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// 假相机适配器：实现 <see cref="ICamera"/> 契约，用于契约测试，不接触任何真实 SDK。
    /// </summary>
    public class FakeCamera : CameraBase
    {
        private double _exposureTimeUs;
        private double _gain;
        private CameraData _frame = new();

        public DeviceConnection? OpenedConnection { get; private set; }

        public bool IsOpen { get; private set; }

        public bool IsAcquiring { get; private set; }

        public int TriggerCount { get; private set; }

        public override string Name => "FakeCamera";

        public void SetNextFrame(CameraData frame) => _frame = frame;

        public override bool Open(DeviceConnection connection)
        {
            OpenedConnection = connection;
            IsOpen = true;
            return true;
        }

        public override void Close()
        {
            IsOpen = false;
            IsAcquiring = false;
        }

        public override void StartAcquisition() => IsAcquiring = true;

        public override void StopAcquisition() => IsAcquiring = false;

        public override CameraData GetOneImage() => _frame;

        public override void SetExposureTime(double exposureTimeUs) => _exposureTimeUs = exposureTimeUs;

        public override double GetExposureTime() => _exposureTimeUs;

        public override void SetGain(double gain) => _gain = gain;

        public override double GetGain() => _gain;

        public override void TriggerSoftware() => TriggerCount++;
    }
}
