using System;
using Microsoft.EntityFrameworkCore;
using SyZero.Domain.Entities;
using SyZero.Domain.Model;
using SyZero.EntityFrameworkCore.Repositories;

namespace SyZero.EntityFrameworkCore.Domain
{
    /// <summary>
    /// 领域模型
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public class DomainModel<TDbContext, TEntity> : EfRepository<TDbContext, TEntity, long>, IDomainModel<TEntity>
        where TEntity : class, IEntity
        where TDbContext : DbContext
    {
        public DomainModel(TDbContext dbContext)
            : base(dbContext)
        {
        }
    }
}
