namespace SyZero.Consul.Config
{
    /// <summary>
    ///  Consul配置模型类
    /// </summary>
    public class ConsulServiceOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "Consul";

        /// <summary>
        /// 服务注册地址（Consul的地址）
        /// </summary>
        public string ConsulAddress { get; set; }

        /// <summary>
        /// 服务ID
        /// </summary>
        public string ServiceId { get; set; }

        /// <summary>
        /// 健康检查地址
        /// </summary>
        public string HealthCheck { get; set; }

        /// <summary>
        /// Acl Token
        /// </summary>
        public string Token { get; set; }

    }
}
