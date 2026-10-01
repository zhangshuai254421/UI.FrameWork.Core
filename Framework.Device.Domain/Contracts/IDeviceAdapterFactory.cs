namespace Framework.Device.Domain
{
    /// <summary>
    /// 适配器工厂：给定 (设备类型, 厂商) 产出对应的适配器实例。
    /// 生产实现按配置动态加载厂商适配器 DLL；测试用假工厂替换，不接触真实 DLL 与硬件。
    /// </summary>
    public interface IDeviceAdapterFactory
    {
        /// <summary>按设备类型与厂商解析并实例化适配器；未注册时抛出异常。</summary>
        IDevice Create(DeviceKind kind, string vendor);
    }
}
