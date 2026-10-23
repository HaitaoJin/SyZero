using System;
using System.Threading.Tasks;
using SyZero.Application.Service;
using SyZero.Application.Service.Dto;
using SyZero.Example1.Core.Entities;
using SyZero.Example1.Core.Repository;
using SyZero.Example1.IApplication.Examples;
using SyZero.Example1.IApplication.Examples.Dto;
using SyZero.Example2.IApplication.Examples;
using SyZero.Dependency;
using SyZero.Runtime.Session;
using Microsoft.Extensions.Options;
using SyZero.Configurations;

namespace SyZero.Example1.Application.Examples
{
    public class Example1AppService : AsyncCrudAppService<Example, Example1Dto, PageAndSortQueryDto, CreateExample1Dto>, IExample1AppService
    {
        private readonly IExampleRepository _exampleRepository;
        private readonly IExample2AppService _example2Service;
        private readonly SyZeroServerOptions _serverOptions;

        public Example1AppService(
            IExampleRepository exampleRepository,
            IExample2AppService example2Service,
            SyZeroServiceDependencies dependencies,
            ISySession sySession,
            IOptions<SyZeroServerOptions> serverOptions) : base(exampleRepository, dependencies, sySession)
        {
            _exampleRepository = exampleRepository;
            _example2Service = example2Service;
            _serverOptions = serverOptions.Value;
        }

        protected override void CheckPermission(string permissionName)
        {
            // Skip permission check
        }

        public async Task<Example1Dto> GetExample(long id)
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
            return ObjectMapper.Map<Example1Dto>(example);
        }

        public async Task<Example1Dto> GetByName(string name)
        {
            var example = await _exampleRepository.GetModelAsync(p => p.Name == name);
            return ObjectMapper.Map<Example1Dto>(example);
        }

        /// <summary>
        /// 获取当前服务信息
        /// </summary>
        public Task<Example1.IApplication.Examples.ServiceInfoDto> GetServiceInfo()
        {
            return Task.FromResult(new Example1.IApplication.Examples.ServiceInfoDto
            {
                ServiceName = _serverOptions.Name,
                Version = "1.0.0",
                Port = _serverOptions.Port,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        /// <summary>
        /// 调用 Example2 服务获取信息
        /// </summary>
        public async Task<Example1.IApplication.Examples.RemoteServiceResultDto> CallExample2Service()
        {
            try
            {
                var remoteInfo = await _example2Service.GetServiceInfo();
                return new Example1.IApplication.Examples.RemoteServiceResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = remoteInfo.ServiceName,
                    RemoteVersion = remoteInfo.Version,
                    RemotePort = remoteInfo.Port,
                    CallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Success = true,
                    Message = "调用 Example2 服务成功"
                };
            }
            catch (Exception ex)
            {
                return new Example1.IApplication.Examples.RemoteServiceResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = "SyZero.Example2",
                    CallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Success = false,
                    Message = $"调用 Example2 服务失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 从 Example2 服务获取示例数据
        /// </summary>
        public async Task<Example1.IApplication.Examples.RemoteExampleResultDto> GetExample2Data(long id)
        {
            try
            {
                var remoteExample = await _example2Service.GetExample(id);
                return new Example1.IApplication.Examples.RemoteExampleResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = "SyZero.Example2",
                    Id = remoteExample.Id,
                    Name = remoteExample.Name,
                    Description = remoteExample.Description,
                    Success = true,
                    Message = "获取 Example2 数据成功"
                };
            }
            catch (Exception ex)
            {
                return new Example1.IApplication.Examples.RemoteExampleResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = "SyZero.Example2",
                    Success = false,
                    Message = $"获取 Example2 数据失败: {ex.Message}"
                };
            }
        }
    }
}
