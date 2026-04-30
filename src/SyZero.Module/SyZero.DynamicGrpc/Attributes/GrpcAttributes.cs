using System;

namespace SyZero.DynamicGrpc.Attributes
{
    /// <summary>
    /// 标记方法不作为 gRPC 方法公开
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public class NonGrpcMethodAttribute : Attribute
    {
    }

    /// <summary>
    /// 标记类型不作为 gRPC 服务公开
    /// 使用此特性可以排除某个 DynamicApi 服务不注册为 gRPC 服务
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class NonGrpcServiceAttribute : Attribute
    {
    }
}
