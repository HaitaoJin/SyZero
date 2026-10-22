namespace SyZero.Service
{
    /// <summary>
    /// 服务注册中心元数据键。
    /// </summary>
    public static class ServiceMetadataKeys
    {
        /// <summary>
        /// 服务接口清单 JSON。
        /// </summary>
        public const string ApiEndpoints = "syzero:api:endpoints";

        /// <summary>
        /// 服务接口数量。
        /// </summary>
        public const string ApiEndpointCount = "syzero:api:endpoint-count";
    }
}
