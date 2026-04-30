namespace SyZero.Domain.Entities
{

    /// <summary>
    /// 定义基本实体类型的接口。系统中的所有实体都必须实现此接口。  常用long类型主键
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// 实体Id
        /// </summary>
        long Id { get; set; }

    }
}
