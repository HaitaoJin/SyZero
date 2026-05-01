namespace SyZero.Client.RemoteService
{
    /// <summary>
    /// 远程服务通用设置
    /// </summary>
    public class RemoteServiceSetting
    {
        /// <summary>
        /// 通信协议（默认 HTTP）
        /// </summary>
        public RemoteServiceProtocol Protocol { get; set; } = RemoteServiceProtocol.Http;

        /// <summary>
        /// 负载均衡策略
        /// </summary>
        public string? Strategy { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int Retry { get; set; } = 0;

        /// <summary>
        /// 超时时间（秒），默认 30 秒
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// 是否启用 SSL/TLS
        /// </summary>
        public bool EnableSsl { get; set; } = false;

        /// <summary>
        /// 最大消息大小（字节），默认 0 表示使用默认值
        /// 主要用于 gRPC 协议
        /// </summary>
        public int MaxMessageSize { get; set; } = 0;
    }
}
