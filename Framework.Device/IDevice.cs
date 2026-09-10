namespace Framework.Device { 
    public interface IDevice
    {
        string Name { get; }

        bool Open();

        void Close();
    }
}
