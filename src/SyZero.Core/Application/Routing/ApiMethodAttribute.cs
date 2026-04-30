using Refit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace SyZero.Application.Routing
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpMethodAttribute : Refit.HttpMethodAttribute
    {
        public HttpMethodAttribute(HttpMethod httpMethod, string Path) : base(RoutingHelper.ApiUrlPre + Path)
        {

            this.httpMethod = httpMethod;
        }

        public HttpMethodAttribute(HttpMethod httpMethod) : this(httpMethod, "")
        {
            this.httpMethod = httpMethod;
        }

        private HttpMethod httpMethod { get; }

        public override HttpMethod Method => httpMethod;
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class GetAttribute : HttpMethodAttribute
    {
        public GetAttribute(string Path) : base(HttpMethod.Get, Path)
        {
        }

        public GetAttribute() : this("")
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class PostAttribute : HttpMethodAttribute
    {
        public PostAttribute(string Path) : base(HttpMethod.Post, Path)
        {
        }

        public PostAttribute() : this("")
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string Path) : base(HttpMethod.Put, Path)
        {
        }

        public PutAttribute() : this("")
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class DeleteAttribute : HttpMethodAttribute
    {
        public DeleteAttribute(string Path) : base(HttpMethod.Delete, Path)
        {
        }

        public DeleteAttribute() : this("")
        {
        }
    }

    /// <summary>
    /// 标记参数为查询字符串参数
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueryAttribute : Refit.QueryAttribute
    {
        public QueryAttribute() : base()
        {
        }

        public QueryAttribute(string name) : base()
        {
            Name = name;
        }

        /// <summary>
        /// 参数名称（可选，默认使用参数变量名）
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 标记参数为请求体
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BodyAttribute : Refit.BodyAttribute
    {
        public BodyAttribute() : base()
        {
        }

        public BodyAttribute(bool buffered) : base(buffered)
        {
        }
    }

    /// <summary>
    /// 标记参数为请求头
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
    public class HeaderAttribute : Refit.HeaderAttribute
    {
        public HeaderAttribute(string header) : base(header)
        {
        }
    }
}
