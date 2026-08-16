namespace FrameWork.Device
{
    /// <summary>
    /// 工业相机接口 - 完整操作契约
    /// </summary>
    public interface ICamera : IDevice
    {
        /// <summary>
        /// 相机能力信息
        /// </summary>
        CameraInfo CameraInfo { get; }

        /// <summary>
        /// 单帧采图（触发模式）
        /// </summary>
        /// <returns>采集到的图像数据</returns>
        Task<ImageAcquiredEventArgs> SnapAsync();

        /// <summary>
        /// 开始连续采集
        /// </summary>
        Task StartContinuousGrabAsync();

        /// <summary>
        /// 停止连续采集
        /// </summary>
        Task StopContinuousGrabAsync();

        /// <summary>
        /// 设置曝光时间（微秒）
        /// </summary>
        /// <param name="exposureUs">曝光时间，单位微秒</param>
        Task SetExposureAsync(double exposureUs);

        /// <summary>
        /// 获取当前曝光时间（微秒）
        /// </summary>
        Task<double> GetExposureAsync();

        /// <summary>
        /// 设置增益（dB 或 倍数，取决于具体相机）
        /// </summary>
        /// <param name="gain">增益值</param>
        Task SetGainAsync(double gain);

        /// <summary>
        /// 获取当前增益值
        /// </summary>
        Task<double> GetGainAsync();

        /// <summary>
        /// 设置触发模式
        /// </summary>
        /// <param name="isTriggerMode">true=触发模式, false=连续模式</param>
        Task SetTriggerModeAsync(bool isTriggerMode);

        /// <summary>
        /// 执行软触发一次
        /// </summary>
        Task SoftTriggerAsync();

        /// <summary>
        /// 图像采集完成事件（连续采集模式下触发）
        /// </summary>
        event EventHandler<ImageAcquiredEventArgs>? ImageAcquired;
    }

    /// <summary>
    /// 相机能力/参数信息
    /// </summary>
    public class CameraInfo
    {
        /// <summary>传感器宽度（像素）</summary>
        public int SensorWidth { get; set; }

        /// <summary>传感器高度（像素）</summary>
        public int SensorHeight { get; set; }

        /// <summary>像素位深</summary>
        public int PixelBitDepth { get; set; } = 8;

        /// <summary>是否支持彩色</summary>
        public bool IsColor { get; set; }

        /// <summary>最小曝光时间（微秒）</summary>
        public double MinExposureUs { get; set; }

        /// <summary>最大曝光时间（微秒）</summary>
        public double MaxExposureUs { get; set; }

        /// <summary>最小增益</summary>
        public double MinGain { get; set; }

        /// <summary>最大增益</summary>
        public double MaxGain { get; set; }

        /// <summary>最大帧率（fps）</summary>
        public double MaxFrameRate { get; set; }

        /// <summary>接口类型（GigE, USB3, CameraLink 等）</summary>
        public string InterfaceType { get; set; } = string.Empty;
    }
}
