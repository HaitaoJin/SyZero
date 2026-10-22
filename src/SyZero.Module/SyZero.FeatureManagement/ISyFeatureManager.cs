using System.Threading.Tasks;
using Microsoft.FeatureManagement;
using SyZero.Dependency;

namespace SyZero.FeatureManagement;

/// <summary>
/// SyZero 特性开关抽象。在业务层直接依赖此接口而非 Microsoft.FeatureManagement，便于替换或 mock。
/// </summary>
public interface ISyFeatureManager
{
    /// <summary>判断指定特性是否启用。</summary>
    Task<bool> IsEnabledAsync(string feature);
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

    public Task<bool> IsEnabledAsync(string feature) => _inner.IsEnabledAsync(feature);
}
