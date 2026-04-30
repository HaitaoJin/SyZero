using Microsoft.Extensions.Configuration;
using NConsul;
using System;
using System.Net.Http;
using System.Threading;

namespace SyZero.Consul
{
    /// <summary>
    /// ConsulConfigurationSource
    /// </summary>
    public interface IConsulConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// CancellationToken
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Consul构造函数实例，可自定义传入
        /// </summary>
        Action<ConsulClientConfiguration> ConsulClientConfiguration { get; set; }

        /// <summary>
        ///  Consul构造函数实例，可自定义传入
        /// </summary>
        Action<HttpClient> ConsulHttpClient { get; set; }

        /// <summary>
        ///  Consul构造函数实例，可自定义传入
        /// </summary>
        Action<HttpClientHandler> ConsulHttpClientHandler { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        string ServiceKey { get; }

        /// <summary>
        /// 可选项
        /// </summary>
        bool Optional { get; set; }

        /// <summary>
        /// Consul查询选项
        /// </summary>
        QueryOptions QueryOptions { get; set; }

        /// <summary>
        /// 重新加载延迟时间，单位是毫秒
        /// </summary>
        int ReloadDelay { get; set; }

        /// <summary>
        /// 是否在配置改变的时候重新加载
        /// </summary>
        bool ReloadOnChange { get; set; }
    }
}