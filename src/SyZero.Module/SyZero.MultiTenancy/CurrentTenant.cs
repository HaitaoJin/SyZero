using System;
using SyZero.Dependency;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 默认 <see cref="ICurrentTenant"/>：基于 AsyncLocal 持有当前租户。
    /// 由 <c>MultiTenancyMiddleware</c> 在每次请求开始时通过 <see cref="Change"/> 写入。
    /// </summary>
    public class CurrentTenant : ICurrentTenant, IScopedDependency
    {
        private static readonly System.Threading.AsyncLocal<TenantInfo?> _current = new();

        public long? Id => _current.Value?.Id;

        public string? Name => _current.Value?.Name;

        public TenantInfo? Tenant => _current.Value;

        public bool IsAvailable => _current.Value is { IsActive: true };

        public IDisposable Change(TenantInfo? tenant)
        {
            var old = _current.Value;
            _current.Value = tenant;
            return new TenantScope(old);
        }

        private sealed class TenantScope : IDisposable
        {
            private readonly TenantInfo? _old;
            private bool _disposed;
            public TenantScope(TenantInfo? old) => _old = old;
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _current.Value = _old;
            }
        }
    }
}
