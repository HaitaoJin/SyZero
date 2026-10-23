using System;
using System.Threading.Tasks;
using SyZero.Application.Service;
using SyZero.Application.Service.Dto;
using SyZero.Example1.IApplication.Examples;
using SyZero.Example2.Core.Entities;
using SyZero.Example2.Core.Repository;
using SyZero.Example2.IApplication.Examples;
using SyZero.Example2.IApplication.Examples.Dto;
using SyZero.Dependency;
using SyZero.Runtime.Session;
using Microsoft.Extensions.Options;
using SyZero.Configurations;

namespace SyZero.Example2.Application.Examples
{
    public class Example2AppService : AsyncCrudAppService<Example, Example2Dto, PageAndSortQueryDto, CreateExample2Dto>, IExample2AppService
    {
        private readonly IExampleRepository _exampleRepository;
        private readonly IExample1AppService _example1Service;
        private readonly SyZeroServerOptions _serverOptions;

        public Example2AppService(
            IExampleRepository exampleRepository,
            IExample1AppService example1Service,
            SyZeroServiceDependencies dependencies,
            ISySession sySession,
            IOptions<SyZeroServerOptions> serverOptions) : base(exampleRepository, dependencies, sySession)
        {
            _exampleRepository = exampleRepository;
            _example1Service = example1Service;
            _serverOptions = serverOptions.Value;
        }

        protected override void CheckPermission(string permissionName)
        {
            // Skip permission check
        }

        public async Task<Example2Dto> GetExample(long id)
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
            return ObjectMapper.Map<Example2Dto>(example);
        }

        public async Task<Example2Dto> GetByName(string name)
        {
            var example = await _exampleRepository.GetModelAsync(p => p.Name == name);
            return ObjectMapper.Map<Example2Dto>(example);
        }

        /// <summary>
        /// 获取当前服务信息
        /// </summary>
        public Task<Example2.IApplication.Examples.ServiceInfoDto> GetServiceInfo()
        {
            return Task.FromResult(new Example2.IApplication.Examples.ServiceInfoDto
            {
                ServiceName = _serverOptions.Name,
                Version = "1.0.0",
                Port = _serverOptions.Port,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        /// <summary>
        /// 调用 Example1 服务获取信息
        /// </summary>
        public async Task<Example2.IApplication.Examples.RemoteServiceResultDto> CallExample1Service()
        {
            try
            {
                var remoteInfo = await _example1Service.GetServiceInfo();
                return new Example2.IApplication.Examples.RemoteServiceResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = remoteInfo.ServiceName,
                    RemoteVersion = remoteInfo.Version,
                    RemotePort = remoteInfo.Port,
                    CallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Success = true,
                    Message = "调用 Example1 服务成功"
                };
            }
            catch (Exception ex)
            {
                return new Example2.IApplication.Examples.RemoteServiceResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = "SyZero.Example1",
                    CallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Success = false,
                    Message = $"调用 Example1 服务失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 从 Example1 服务获取示例数据
        /// </summary>
        public async Task<Example2.IApplication.Examples.RemoteExampleResultDto> GetExample1Data(long id)
        {
            try
            {
                var remoteExample = await _example1Service.GetExample(id);
                return new Example2.IApplication.Examples.RemoteExampleResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = "SyZero.Example1",
                    Id = remoteExample.Id,
                    Name = remoteExample.Name,
                    Description = remoteExample.Description,
                    Success = true,
                    Message = "获取 Example1 数据成功"
                };
            }
            catch (Exception ex)
            {
                return new Example2.IApplication.Examples.RemoteExampleResultDto
                {
                    LocalService = _serverOptions.Name,
                    RemoteService = "SyZero.Example1",
                    Success = false,
                    Message = $"获取 Example1 数据失败: {ex.Message}"
                };
            }
        }
    }
}
