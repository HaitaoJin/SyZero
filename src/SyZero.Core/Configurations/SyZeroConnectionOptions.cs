using System.Collections.Generic;
using SyZero.Runtime.Security;

namespace SyZero.Configurations
{
    /// <summary>
    /// SyZero连接字符串配置
    /// </summary>
    public class SyZeroConnectionOptions
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DbType Type { get; set; }

        /// <summary>
        /// 主数据库
        /// </summary>
        public string Master { get; set; }

        /// <summary>
        /// 从数据库 连接字符串,权重
        /// </summary>
        public List<SlaveConnectionOptions> Slave { get; set; } = new List<SlaveConnectionOptions>();
    }

    /// <summary>
    /// 备数据库
    /// </summary>
    public class SlaveConnectionOptions
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 权重
        /// </summary>
        public int HitRate { get; set; } = 0;
    }
}
