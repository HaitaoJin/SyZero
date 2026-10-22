namespace SyZero.Client.RemoteService
{
    /// <summary>
    /// 远程服务条目（单个被调用服务的描述）
    /// </summary>
    public class RemoteServiceItem : RemoteServiceSetting
    {
        /// <summary>
        /// 注册中心中的服务名称
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// 接口契约所在的程序集名称
        /// （Provider 据此匹配应当为哪些接口生成代理）
        /// </summary>
        public string AssemblyName { get; set; } = string.Empty;
    }
}
