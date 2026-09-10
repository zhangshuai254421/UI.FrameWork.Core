namespace Framework.Device
{
    /// <summary>
    /// 连接方式：程序抵达设备的方式，TCP（IP + 端口）或 COM（串口号）。
    /// </summary>
    public abstract class DeviceConnection
    {
    }

    /// <summary>
    /// TCP 连接：IP 地址 + 端口号。
    /// </summary>
    public sealed class TcpConnection : DeviceConnection
    {
        public TcpConnection(string ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public string IpAddress { get; }

        public int Port { get; }
    }

    /// <summary>
    /// COM 连接：串口号。
    /// </summary>
    public sealed class ComConnection : DeviceConnection
    {
        public ComConnection(string portName)
        {
            PortName = portName;
        }

        public string PortName { get; }
    }
}
