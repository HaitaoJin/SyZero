using SyZero.Application.Service.Dto;

namespace SyZero.Example3.IApplication.Examples.Dto
{
    public class CreateExample3Dto : EntityDto
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
    }
}
