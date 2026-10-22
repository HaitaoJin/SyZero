using System;
using System.Collections.Generic;

namespace SyZero.Client.RemoteService
{
    /// <summary>
    /// 远程服务调用配置
    /// </summary>
    public class RemoteServiceOptions
    {
        /// <summary>
        /// 默认配置节名称
        /// </summary>
        public const string SectionName = "RemoteService";

        /// <summary>
        /// 服务条目列表
        /// </summary>
        public List<RemoteServiceItem> Service { get; set; } = new List<RemoteServiceItem>();

        /// <summary>
        /// 全局默认配置
        /// </summary>
        public RemoteServiceSetting Global { get; set; } = new RemoteServiceSetting();

        /// <summary>
        /// 校验配置项
        /// </summary>
        public void Validate()
        {
            if (Service == null)
            {
                Service = new List<RemoteServiceItem>();
            }

            foreach (var service in Service)
            {
                if (string.IsNullOrWhiteSpace(service.ServiceName))
                {
                    throw new ArgumentException("RemoteServiceItem.ServiceName 不能为空");
                }
                if (string.IsNullOrWhiteSpace(service.AssemblyName))
                {
                    throw new ArgumentException("RemoteServiceItem.AssemblyName 不能为空");
                }
            }
        }
    }
}
