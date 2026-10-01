namespace Framework.Device.Domain
{
    /// <summary>
    /// 连接方式（持久化用扁平判别）：TCP 或 COM，与契约里的 TcpConnection/ComConnection 一一对应。
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>TCP 网络连接：基于 IP 地址与端口的以太网连接（工业相机、网口设备常用）。</summary>
        Tcp = 1,
        /// <summary>COM 串口连接：基于串口号与波特率的串行连接（控制卡、串口设备常用）。</summary>
        Com = 2,
    }
}
