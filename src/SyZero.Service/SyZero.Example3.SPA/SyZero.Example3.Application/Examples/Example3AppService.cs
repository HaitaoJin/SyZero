using System;
using System.Threading.Tasks;
using SyZero.Application.Service;
using SyZero.Application.Service.Dto;
using SyZero.Example3.Core.Entities;
using SyZero.Example3.Core.Repository;
using SyZero.Example3.IApplication.Examples;
using SyZero.Example3.IApplication.Examples.Dto;
using SyZero.Dependency;
using SyZero.Runtime.Session;
using Microsoft.Extensions.Options;
using SyZero.Configurations;

namespace SyZero.Example3.Application.Examples
{
    public class Example3AppService : AsyncCrudAppService<Example, Example3Dto, PageAndSortQueryDto, CreateExample3Dto>, IExample3AppService
    {
        private readonly IExampleRepository _exampleRepository;
        private readonly SyZeroServerOptions _serverOptions;

        public Example3AppService(IExampleRepository exampleRepository, SyZeroServiceDependencies dependencies, ISySession sySession, IOptions<SyZeroServerOptions> serverOptions)
            : base(exampleRepository, dependencies, sySession)
        {
            _exampleRepository = exampleRepository;
            _serverOptions = serverOptions.Value;
        }

        protected override void CheckPermission(string permissionName)
        {
            // 示例项目跳过权限校验
        }

        public async Task<Example3Dto> GetExample(long id)
        {
            var example = await _exampleRepository.GetModelAsync(p => p.Id == id);
            if (example == null)
            {
                example = await _exampleRepository.AddAsync(new Example
                {
                    Id = id,
                    Name = "Test",
                    Description = "This is a test example",
                    Status = 1,
                    Sort = 1
                });
            }
            return ObjectMapper.Map<Example3Dto>(example);
        }

        public async Task<Example3Dto> GetByName(string name)
        {
            var example = await _exampleRepository.GetModelAsync(p => p.Name == name);
            return ObjectMapper.Map<Example3Dto>(example);
        }

        /// <summary>
        /// 获取当前服务信息
        /// </summary>
        public Task<ServiceInfoDto> GetServiceInfo()
        {
            return Task.FromResult(new ServiceInfoDto
            {
                ServiceName = _serverOptions.Name,
                Version = "1.0.0",
                Port = _serverOptions.Port,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }
}
