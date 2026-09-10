namespace Framework.Device
{
    public abstract class DeviceBase : IDevice
    {
        public abstract string Name { get; }

        public abstract bool Open(DeviceConnection connection);

        public abstract void Close();
    }
}
