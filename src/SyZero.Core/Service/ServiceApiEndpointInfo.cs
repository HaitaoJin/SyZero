namespace SyZero.Service
{
    /// <summary>
    /// 服务注册时上报的接口摘要。
    /// </summary>
    public sealed class ServiceApiEndpointInfo
    {
        /// <summary>
        /// HTTP 方法。
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// 路径模板。
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 分组名。
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 控制器名。
        /// </summary>
        public string? Controller { get; set; }

        /// <summary>
        /// Action 名。
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// 路由显示名。
        /// </summary>
        public string? DisplayName { get; set; }
    }
}
