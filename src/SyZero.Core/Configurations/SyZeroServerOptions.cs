using SyZero.Runtime.Security;

namespace SyZero.Configurations
{
    /// <summary>
    /// SyZero配置
    /// </summary>
    public class SyZeroServerOptions
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 服务内网Ip
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// 服务端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 服务广域网Ip
        /// </summary>
        public string WanIp { get; set; }

        /// <summary>
        /// 协议 HTTP/HTTPS/GRPC
        /// </summary>
        public ProtocolType Protocol { get; set; } = ProtocolType.HTTP;

        /// <summary>
        /// 检查时间间隔 （秒）
        /// </summary>
        public int InspectInterval { get; set; } = 10;

        /// <summary>
        /// 开放地址
        /// </summary>
        public string CorsOrigins { get; set; }

        /// <summary>
        /// 动态配置
        /// </summary>
        public string DynamicConfig
        {
            get;
            set;
        }
    }
}
