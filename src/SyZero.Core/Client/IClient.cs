using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SyZero.Client
{
    /// <summary>
    /// 客户端IClient
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="requestTemplate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ResponseTemplate<T>> ExecuteAsync<T>(RequestTemplate requestTemplate, CancellationToken cancellationToken);

    }
}
