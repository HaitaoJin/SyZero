namespace SyZero.Application.DynamicApi
{
    /// <summary>
    /// 动态 API Provider 选项的可选公共基类（轻量契约）。
    /// 提供 <see cref="DefaultAreaName"/> 与 <see cref="Validate"/> 模板，让 Provider 自身保留差异化字段。
    /// </summary>
    /// <remarks>
    /// 字段级复用故意保持最小，避免与既有公开属性（如 <c>RemoveControllerPostfixes</c> /
    /// <c>RemoveServicePostfixes</c>、<c>AssemblyOptions</c> 的具体类型）发生破坏性变更。
    /// 通用算法请直接调用 <see cref="DynamicApiNameNormalizer"/>。
    /// </remarks>
    public abstract class DynamicApiOptionsBase
    {
        /// <summary>
        /// 默认区域 / 模块名（写入路由的 area 段或 gRPC 服务名的模块部分）。
        /// </summary>
        public string DefaultAreaName { get; set; } = string.Empty;

        /// <summary>
        /// 校验配置合法性。子类实现以追加自身校验。
        /// </summary>
        public abstract void Validate();
    }
}
