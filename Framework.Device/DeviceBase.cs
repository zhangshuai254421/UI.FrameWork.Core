namespace Framework.Device
{
    public abstract class DeviceBase : IDevice
    {
        public abstract string Name { get; }
        public abstract bool Open();
        public abstract void Close();
    }
}
