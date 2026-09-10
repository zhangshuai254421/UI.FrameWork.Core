namespace Framework.Device.Domain
{
    /// <summary>
    /// 连接方式（持久化用扁平判别）：TCP 或 COM，与契约里的 TcpConnection/ComConnection 一一对应。
    /// </summary>
    public enum ConnectionType
    {
        Tcp = 1,
        Com = 2,
    }
}
