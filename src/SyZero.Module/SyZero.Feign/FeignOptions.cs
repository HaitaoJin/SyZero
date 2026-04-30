using System;
using System.Collections.Generic;
using System.Text;

namespace SyZero.Feign
{
    /// <summary>
    /// 通信协议类型
    /// </summary>
    public enum FeignProtocol
    {
        /// <summary>
        /// HTTP 协议
        /// </summary>
        Http = 0,

        /// <summary>
        /// gRPC 协议
        /// </summary>
        Grpc = 1
    }

    /// <summary>
    /// Feign 配置选项
    /// </summary>
    public class FeignOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "Feign";

        /// <summary>
        /// 服务配置列表
        /// </summary>
        public List<FeignService> Service { get; set; } = new List<FeignService>();

        /// <summary>
        /// 全局配置
        /// </summary>
        public ServiceSetting Global { get; set; } = new ServiceSetting();

        /// <summary>
        /// 验证配置
        /// </summary>
        public void Validate()
        {
            if (Service == null)
            {
                Service = new List<FeignService>();
            }

            foreach (var service in Service)
            {
                if (string.IsNullOrWhiteSpace(service.ServiceName))
                {
                    throw new ArgumentException("FeignService.ServiceName 不能为空");
                }
                if (string.IsNullOrWhiteSpace(service.DllName))
                {
                    throw new ArgumentException("FeignService.DllName 不能为空");
                }
            }
        }
    }

    /// <summary>
    /// Feign 服务配置
    /// </summary>
    public class FeignService : ServiceSetting
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// DLL 名称
        /// </summary>
        public string DllName { get; set; }
    }

    /// <summary>
    /// 服务设置
    /// </summary>
    public class ServiceSetting
    {
        /// <summary>
        /// 通信协议（默认 HTTP）
        /// </summary>
        public FeignProtocol Protocol { get; set; } = FeignProtocol.Http;

        /// <summary>
        /// 负载均衡策略
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int Retry { get; set; } = 0;

        /// <summary>
        /// 超时时间（秒），默认 30 秒
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// 是否启用 SSL/TLS
        /// </summary>
        public bool EnableSsl { get; set; } = false;

        /// <summary>
        /// 最大消息大小（字节），默认 0 表示使用默认值
        /// 主要用于 gRPC 协议
        /// </summary>
        public int MaxMessageSize { get; set; } = 0;
    }
}
