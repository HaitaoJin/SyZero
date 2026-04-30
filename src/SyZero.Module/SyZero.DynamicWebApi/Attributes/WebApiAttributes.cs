using System;

namespace SyZero.DynamicWebApi.Attributes
{
    /// <summary>
    /// 标记类型不作为 WebApi 控制器公开
    /// 使用此特性可以排除某个 DynamicApi 服务不注册为 WebApi
    /// </summary>
    /// <remarks>
    /// 注意：也可以使用 NonDynamicApiAttribute 来同时排除 WebApi 和 gRPC
    /// </remarks>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class NonWebApiServiceAttribute : Attribute
    {
    }

    /// <summary>
    /// 标记方法不作为 WebApi Action 公开
    /// </summary>
    /// <remarks>
    /// 注意：也可以使用 NonDynamicMethodAttribute 来排除方法
    /// </remarks>
    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public class NonWebApiMethodAttribute : Attribute
    {
    }
}
