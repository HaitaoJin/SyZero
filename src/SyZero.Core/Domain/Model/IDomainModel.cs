using SyZero.Domain.Entities;
using SyZero.Domain.Repository;

namespace SyZero.Domain.Model
{
    /// <summary>
    /// 领域Model
    /// </summary>
    public interface IDomainModel<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {

    }
}
