using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyZero.Domain.Repository;
using SyZero.Service.DBServiceManagement.Entity;

namespace SyZero.Service.DBServiceManagement.Repository
{
    /// <summary>
    /// 服务注册仓储默认实现
    /// </summary>
    public class ServiceRegistryRepository : IServiceRegistryRepository
    {
        private readonly IRepository<ServiceRegistryEntity> _repository;

        public ServiceRegistryRepository(IRepository<ServiceRegistryEntity> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<ServiceRegistryEntity>> GetByServiceNameAsync(string serviceName)
        {
            var query = await _repository.GetListAsync(x => x.ServiceName == serviceName);
            return query.ToList();
        }

        public async Task<List<ServiceRegistryEntity>> GetHealthyByServiceNameAsync(string serviceName, int expireSeconds)
        {
            var expireTime = DateTime.UtcNow.AddSeconds(-expireSeconds);
            var query = await _repository.GetListAsync(x =>
                x.ServiceName == serviceName &&
                x.IsHealthy &&
                x.Enabled &&
                x.LastHeartbeat >= expireTime);
            return query.ToList();
        }

        public async Task<ServiceRegistryEntity> GetByServiceIdAsync(string serviceId)
        {
            return await _repository.GetModelAsync(x => x.ServiceID == serviceId);
        }

        public async Task<List<string>> GetAllServiceNamesAsync()
        {
            var query = await _repository.GetListAsync();
            return query.Select(x => x.ServiceName).Distinct().ToList();
        }

        public async Task RegisterAsync(ServiceRegistryEntity entity)
        {
            await _repository.AddAsync(entity);
        }

        public async Task DeregisterAsync(string serviceId)
        {
            var entity = await _repository.GetModelAsync(x => x.ServiceID == serviceId);
            if (entity != null)
            {
                await _repository.DeleteAsync(entity.Id);
            }
        }

        public async Task UpdateHeartbeatAsync(string serviceId)
        {
            var entity = await _repository.GetModelAsync(x => x.ServiceID == serviceId);
            if (entity != null)
            {
                entity.LastHeartbeat = DateTime.UtcNow;
                await _repository.UpdateAsync(entity);
            }
        }

        public async Task UpdateHealthStatusAsync(string serviceId, bool isHealthy)
        {
            var entity = await _repository.GetModelAsync(x => x.ServiceID == serviceId);
            if (entity != null)
            {
                entity.IsHealthy = isHealthy;
                await _repository.UpdateAsync(entity);
            }
        }

        public async Task CleanExpiredServicesAsync(int expireSeconds)
        {
            var expireTime = DateTime.UtcNow.AddSeconds(-expireSeconds);
            var query = await _repository.GetListAsync(x => x.LastHeartbeat < expireTime);
            var expiredEntities = query.ToList();

            foreach (var entity in expiredEntities)
            {
                await _repository.DeleteAsync(entity.Id);
            }
        }
    }

}
