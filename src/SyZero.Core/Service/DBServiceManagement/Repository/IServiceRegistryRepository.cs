using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.Service.DBServiceManagement.Entity;

namespace SyZero.Service.DBServiceManagement.Repository
{
    /// <summary>
    /// 服务注册仓储接口
    /// </summary>
    public interface IServiceRegistryRepository
    {
        Task<List<ServiceRegistryEntity>> GetByServiceNameAsync(string serviceName);
        Task<List<ServiceRegistryEntity>> GetHealthyByServiceNameAsync(string serviceName, int expireSeconds);
        Task<ServiceRegistryEntity> GetByServiceIdAsync(string serviceId);
        Task<List<string>> GetAllServiceNamesAsync();
        Task RegisterAsync(ServiceRegistryEntity entity);
        Task DeregisterAsync(string serviceId);
        Task UpdateHeartbeatAsync(string serviceId);
        Task UpdateHealthStatusAsync(string serviceId, bool isHealthy);
        Task CleanExpiredServicesAsync(int expireSeconds);
    }

}
