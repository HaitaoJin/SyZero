using SyZero.Domain.Entities;

namespace SyZero.Domain.Repository
{
    /// <summary>
    /// 通用仓储接口（自定义主键类型）。
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
    }

    /// <summary>
    /// 通用仓储接口（默认 long 主键，兼容历史用法）。
    /// </summary>
    public interface IRepository<TEntity> : IRepository<TEntity, long> where TEntity : class, IEntity
    {
    }
}
