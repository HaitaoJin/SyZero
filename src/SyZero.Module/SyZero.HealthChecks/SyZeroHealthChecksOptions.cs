using Microsoft.Extensions.Configuration;

namespace SyZero.HealthChecks
{
    /// <summary>
    /// SyZero.HealthChecks 配置选项。
    /// </summary>
    public class SyZeroHealthChecksOptions
    {
        /// <summary>
        /// 配置节名称。
        /// </summary>
        public const string SectionName = "SyZeroHealthChecks";

        /// <summary>
        /// 存活探针路径，默认 /health/live。仅返回进程存活信息（不依赖外部资源）。
        /// </summary>
        public string LivePath { get; set; } = "/health/live";

        /// <summary>
        /// 就绪探针路径，默认 /health/ready。检查所有依赖资源（标签为 "ready" 的检查器）。
        /// </summary>
        public string ReadyPath { get; set; } = "/health/ready";

        /// <summary>
        /// 综合健康端点路径，默认 /health。运行所有已注册的检查器。
        /// </summary>
        public string OverallPath { get; set; } = "/health";

        /// <summary>
        /// 是否在响应中输出详细的 JSON（包含每项检查名称、状态、耗时、Tags、Description）。默认 true。
        /// </summary>
        public bool DetailedResponse { get; set; } = true;
    }
}
