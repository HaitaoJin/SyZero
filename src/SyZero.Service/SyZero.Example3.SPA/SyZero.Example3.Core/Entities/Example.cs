using System;
using SyZero.Domain.Entities;

namespace SyZero.Example3.Core.Entities
{
    /// <summary>
    /// 示例实体
    /// </summary>
    public class Example : Entity
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 状态 0:禁用 1:启用
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }
}
