using Microsoft.Extensions.Localization;
using SyZero.Dependency;

namespace SyZero.Localization;

/// <summary>
/// SyZero 字符串本地化抽象，业务直接依赖此接口而非 Microsoft.Extensions.Localization。
/// </summary>
public interface ISyStringLocalizer
{
    /// <summary>按 key 取本地化字符串；缺失时返回 key 本身。</summary>
    string this[string key] { get; }

    /// <summary>带格式化参数的本地化。</summary>
    string this[string key, params object[] args] { get; }
}

/// <summary>
/// 包装 <see cref="IStringLocalizerFactory"/> 的默认实现，使用单一资源类型 <see cref="SharedResource"/>。
/// </summary>
public class DefaultSyStringLocalizer : ISyStringLocalizer, IScopedDependency
{
    private readonly IStringLocalizer _inner;

    public DefaultSyStringLocalizer(IStringLocalizerFactory factory)
    {
        _inner = factory.Create(typeof(SharedResource));
    }

    public string this[string key] => _inner[key].Value;

    public string this[string key, params object[] args] => _inner[key, args].Value;
}

/// <summary>共享资源类型占位（业务可放 Resources/SharedResource.zh-CN.resx 等）。</summary>
public class SharedResource { }
