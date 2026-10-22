namespace SyZero.Domain.Entities
{
    /// <summary>
    /// 定义带泛型主键的实体接口。系统中的所有实体建议实现此接口或继承自 <see cref="IEntity"/>。
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IEntity<TKey>
    {
        /// <summary>
        /// 实体主键
        /// </summary>
        TKey Id { get; set; }
    }

    /// <summary>
    /// 定义基本实体类型的接口（默认 long 主键，兼容历史用法）。
    /// </summary>
    public interface IEntity : IEntity<long>
    {
    }
}
