namespace Framework.Device.Domain
{
    /// <summary>
    /// 相机取流模式。
    /// </summary>
    public enum CameraGrabMode
    {
        /// <summary>主动取流：StartAcquisition 后由调用方 <see cref="ICamera.GetOneImage"/> 拉帧（软触发拍照场景）。</summary>
        Pull = 0,

        /// <summary>回调取流：StartAcquisition 后 SDK 持续推帧到 <see cref="CameraBase.ChannelCameraData"/>（连续预览场景）。</summary>
        Callback = 1,
    }
}
