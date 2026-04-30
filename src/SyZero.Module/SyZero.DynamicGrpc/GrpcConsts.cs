using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SyZero.DynamicGrpc
{
    /// <summary>
    /// Dynamic gRPC 常量和默认配置
    /// </summary>
    public static class GrpcConsts
    {
        /// <summary>
        /// 默认服务后缀（将被移除）
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultServicePostfixes =
            new ReadOnlyCollection<string>(new List<string> { "AppService", "ApplicationService", "Service", "GrpcService" });

        /// <summary>
        /// 默认方法后缀（将被移除）
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultMethodPostfixes =
            new ReadOnlyCollection<string>(new List<string> { "Async" });

        /// <summary>
        /// 默认 gRPC 服务前缀
        /// </summary>
        public const string DefaultServicePrefix = "";

        /// <summary>
        /// 默认最大消息大小（4MB）
        /// </summary>
        public const int DefaultMaxMessageSize = 4 * 1024 * 1024;

        /// <summary>
        /// 默认超时时间（秒）
        /// </summary>
        public const int DefaultTimeoutSeconds = 30;
    }
}
