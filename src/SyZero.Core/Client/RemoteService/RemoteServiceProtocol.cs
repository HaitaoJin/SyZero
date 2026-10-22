namespace SyZero.Client.RemoteService
{
    /// <summary>
    /// 远程服务通信协议类型
    /// </summary>
    public enum RemoteServiceProtocol
    {
        /// <summary>
        /// HTTP 协议
        /// </summary>
        Http = 0,

        /// <summary>
        /// gRPC 协议
        /// </summary>
        Grpc = 1
    }
}
