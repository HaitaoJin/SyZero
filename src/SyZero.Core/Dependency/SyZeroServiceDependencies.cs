using Microsoft.Extensions.Logging;
using SyZero.Domain.Repository;
using SyZero.ObjectMapper;
using SyZero.Runtime.Security;

namespace SyZero.Dependency;

/// <summary>应用服务的显式依赖集合，用于替代静态服务定位器。</summary>
public sealed class SyZeroServiceDependencies(
    IUnitOfWork unitOfWork,
    ILogger<SyZeroServiceBase> logger,
    IObjectMapper objectMapper,
    IPermissionChecker permissionChecker) : IScopedDependency
{
    public IUnitOfWork UnitOfWork { get; } = unitOfWork;
    public ILogger<SyZeroServiceBase> Logger { get; } = logger;
    public IObjectMapper ObjectMapper { get; } = objectMapper;
    public IPermissionChecker PermissionChecker { get; } = permissionChecker;
}
