using SyZero.Domain.Entities;

namespace SyZero.Domain.Repository
{

    public interface IRepository<TEntity> : IBaseRepository<TEntity, long> where TEntity : class, IEntity
    {

    }
}
