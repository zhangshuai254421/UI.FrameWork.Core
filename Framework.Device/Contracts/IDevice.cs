namespace Framework.Device
{
    /// <summary>
    /// 极薄的设备根契约：任何硬件（相机、运动控制卡等）的最小公共能力。
    /// </summary>
    public interface IDevice
    {
        string Name { get; }

        bool Open(DeviceConnection connection);

        void Close();
    }
}
