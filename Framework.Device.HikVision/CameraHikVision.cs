namespace Framework.Device.HikVision
{
    /// <summary>
    /// 海康相机适配器（骨架）。已改挂到品牌无关的 <see cref="ICamera"/> 契约，
    /// 真实 SDK 调用在后续工单实现。
    /// </summary>
    public class CameraHikVision : CameraBase
    {
        public override string Name => "HikVisionCamera";

        public override bool Open(DeviceConnection connection)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

        public override void StartAcquisition()
        {
            throw new NotImplementedException();
        }

        public override void StopAcquisition()
        {
            throw new NotImplementedException();
        }

        public override CameraData GetOneImage()
        {
            throw new NotImplementedException();
        }

        public override void SetExposureTime(double exposureTimeUs)
        {
            throw new NotImplementedException();
        }

        public override double GetExposureTime()
        {
            throw new NotImplementedException();
        }

        public override void SetGain(double gain)
        {
            throw new NotImplementedException();
        }

        public override double GetGain()
        {
            throw new NotImplementedException();
        }

        public override void TriggerSoftware()
        {
            throw new NotImplementedException();
        }
    }
}
