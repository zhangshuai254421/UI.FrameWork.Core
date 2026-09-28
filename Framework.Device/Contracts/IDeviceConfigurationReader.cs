namespace Framework.Device
{
    /// <summary>
    /// 设备配置读取器：提供设备管理器启动时所需的配置来源。
    /// 生产实现从数据库读；测试用内存实现替换，不接触数据库。
    /// </summary>
    public interface IDeviceConfigurationReader
    {
        /// <summary>读取全部设备配置；返回顺序即初始化顺序。</summary>
        IReadOnlyList<DeviceConfiguration> Read();
    }
}
