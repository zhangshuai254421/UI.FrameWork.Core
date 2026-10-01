namespace Framework.Device.Domain
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
        /// <summary>创建 TCP 连接。</summary>
        public TcpConnection(string ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        /// <summary>设备 IP 地址。</summary>
        public string IpAddress { get; }

        /// <summary>端口号。</summary>
        public int Port { get; }

        /// <summary>渲染为 "TCP {IP}:{端口}"，供日志与诊断展示。</summary>
        public override string ToString() => $"TCP {IpAddress}:{Port}";
    }

    /// <summary>
    /// COM 连接：串口号。
    /// </summary>
    public sealed class ComConnection : DeviceConnection
    {
        /// <summary>创建 COM 连接。</summary>
        public ComConnection(string portName)
        {
            PortName = portName;
        }

        /// <summary>串口号（如 COM3）。</summary>
        public string PortName { get; }

        /// <summary>渲染为串口号本身（如 "COM3"），供日志与诊断展示。</summary>
        public override string ToString() => PortName;
    }
}
