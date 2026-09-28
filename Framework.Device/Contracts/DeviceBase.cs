namespace Framework.Device
{
    /// <summary>
    /// 设备适配器基类：打开 / 关闭的公共骨架，相机、运动控制卡等基类从这里派生。
    /// </summary>
    public abstract class DeviceBase : IDevice
    {
        /// <summary>适配器名（日志与展示用）。</summary>
        public abstract string Name { get; }

        /// <summary>按连接方式打开设备；成功返回 true，失败原因由具体适配器暴露。</summary>
        public abstract bool Open(DeviceConnection connection);

        /// <summary>关闭设备并释放资源；未打开时应为空操作，可重复调用。</summary>
        public abstract void Close();
    }
}
