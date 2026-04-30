namespace SyZero.DynamicGrpc
{
    /// <summary>
    /// 程序集级别的 Dynamic gRPC 选项
    /// </summary>
    public class AssemblyDynamicGrpcOptions
    {
        /// <summary>
        /// 服务前缀
        /// <para>默认值: null</para>
        /// </summary>
        public string ServicePrefix { get; }

        /// <summary>
        /// 初始化程序集级别选项
        /// </summary>
        /// <param name="servicePrefix">服务前缀</param>
        public AssemblyDynamicGrpcOptions(string servicePrefix = null)
        {
            ServicePrefix = servicePrefix;
        }
    }
}
