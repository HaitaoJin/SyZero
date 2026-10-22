using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SyZero.Application.DynamicApi;

namespace SyZero.DynamicGrpc
{
    /// <summary>
    /// Dynamic gRPC 服务发现辅助类
    /// </summary>
    internal static class DynamicGrpcServiceDiscovery
    {
        /// <summary>
        /// 获取所有可注册的 Dynamic gRPC 服务类型
        /// </summary>
        public static IReadOnlyList<Type> GetServiceTypes(DynamicGrpcOptions options, DynamicGrpcServiceTypeProvider typeProvider = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            typeProvider ??= new DynamicGrpcServiceTypeProvider(options);

            return GetCandidateAssemblies(options)
                .SelectMany(DynamicApiAssemblyScanner.GetLoadableTypes)
                .Where(type => type != null)
                .Select(type => type.GetTypeInfo())
                .Where(typeProvider.IsGrpcService)
                .Select(typeInfo => typeInfo.AsType())
                .Distinct()
                .ToArray();
        }

        private static IEnumerable<Assembly> GetCandidateAssemblies(DynamicGrpcOptions options)
        {
            if (options.AssemblyOptions.Count > 0)
            {
                return options.AssemblyOptions.Keys;
            }

            // gRPC 场景额外排除 Grpc.* / Google.* 库
            return DynamicApiAssemblyScanner.GetAssemblies(asm =>
            {
                var name = asm.GetName().Name;
                return name != null && (
                    name.StartsWith("Grpc", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Google", StringComparison.OrdinalIgnoreCase));
            });
        }
    }
}
