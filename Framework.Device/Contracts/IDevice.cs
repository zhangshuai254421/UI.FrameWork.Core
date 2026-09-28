namespace Framework.Device
{
    /// <summary>
    /// 极薄的设备根契约：任何硬件（相机、运动控制卡等）的最小公共能力。
    /// </summary>
    public interface IDevice
    {
        /// <summary>设备名（适配器自述，用于日志与展示；不作为唯一标识，唯一标识是配置里的 Id）。</summary>
        string Name { get; }

        /// <summary>按连接方式打开设备；成功返回 true，失败原因由具体适配器暴露（如相机的 LastError）。</summary>
        bool Open(DeviceConnection connection);

        /// <summary>关闭设备并释放资源；未打开时应为空操作，可重复调用。</summary>
        void Close();
    }
}
