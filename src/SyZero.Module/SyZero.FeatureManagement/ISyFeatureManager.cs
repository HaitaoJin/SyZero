using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Microsoft.FeatureManagement;
using SyZero.Dependency;

namespace SyZero.FeatureManagement;

/// <summary>
/// SyZero 特性开关抽象。在业务层直接依赖此接口而非 Microsoft.FeatureManagement，便于替换或 mock。
/// </summary>
public interface ISyFeatureManager
{
    /// <summary>判断指定特性是否启用。</summary>
    Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default);
    Task<bool> IsEnabledAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GetFeatureNamesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 包装 <see cref="IFeatureManager"/> 的默认实现。
/// </summary>
public class DefaultSyFeatureManager : ISyFeatureManager, IScopedDependency
{
    private readonly IFeatureManager _inner;

    public DefaultSyFeatureManager(IFeatureManager inner)
    {
        _inner = inner;
    }

    public Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default)
        => _inner.IsEnabledAsync(feature, cancellationToken);

    public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken = default)
        => _inner.IsEnabledAsync(feature, context);

    public IAsyncEnumerable<string> GetFeatureNamesAsync(CancellationToken cancellationToken = default)
        => _inner.GetFeatureNamesAsync();
}
