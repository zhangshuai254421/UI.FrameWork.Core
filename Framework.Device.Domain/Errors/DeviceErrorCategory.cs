namespace Framework.Device.Domain
{
    /// <summary>
    /// 统一设备错误类别：适配器把厂商错误码翻译成这些类别，供上层判读与降级。
    /// </summary>
    public enum DeviceErrorCategory
    {
        /// <summary>无错误。</summary>
        None = 0,

        /// <summary>找不到设备。</summary>
        DeviceNotFound = 1,

        /// <summary>打开失败。</summary>
        OpenFailed = 2,

        /// <summary>参数不支持。</summary>
        ParameterNotSupported = 3,

        /// <summary>其他未分类错误。</summary>
        Unknown = 4,
    }
}
