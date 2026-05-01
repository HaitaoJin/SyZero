using System.Threading.Tasks;
using SyZero.Application.Routing;
using SyZero.Application.Service;
using SyZero.Application.Service.Dto;
using SyZero.Example3.IApplication.Examples.Dto;

namespace SyZero.Example3.IApplication.Examples
{
    public interface IExample3AppService : IAsyncCrudAppService<Example3Dto, PageAndSortQueryDto, CreateExample3Dto>, IApplicationServiceBase
    {
        /// <summary>
        /// 根据 ID 获取示例
        /// </summary>
        /// <param name="id">示例 ID</param>
        [Get("GetExample")]
        Task<Example3Dto> GetExample(long id);

        /// <summary>
        /// 根据名称获取示例
        /// </summary>
        /// <param name="name">名称</param>
        [Get("GetByName")]
        Task<Example3Dto> GetByName(string name);

        /// <summary>
        /// 获取当前服务信息（演示 SPA 单体宿主）
        /// </summary>
        [Get("GetServiceInfo")]
        Task<ServiceInfoDto> GetServiceInfo();
    }

    /// <summary>
    /// 服务信息 DTO
    /// </summary>
    public class ServiceInfoDto
    {
        public string ServiceName { get; set; }
        public string Version { get; set; }
        public int Port { get; set; }
        public string Timestamp { get; set; }
    }
}
