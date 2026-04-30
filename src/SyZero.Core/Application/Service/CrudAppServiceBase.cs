using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using SyZero.Application.Service.Dto;
using SyZero.Domain.Entities;
using SyZero.Domain.Repository;
using SyZero.ObjectMapper;

namespace SyZero.Application.Service
{
    /// <summary>
    /// This is a common base class for CrudAppService and AsyncCrudAppService classes.
    /// Inherit either from CrudAppService or AsyncCrudAppService, not from this class.
    /// </summary>
    public abstract class CrudAppServiceBase<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput> : ApplicationService
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
        where TUpdateInput : IEntityDto
    {
        protected readonly IRepository<TEntity> Repository;

        protected virtual string GetPermissionName { get; set; }

        protected virtual string GetAllPermissionName { get; set; }

        protected virtual string CreatePermissionName { get; set; }

        protected virtual string UpdatePermissionName { get; set; }

        protected virtual string DeletePermissionName { get; set; }

        protected CrudAppServiceBase(IRepository<TEntity> repository)
        {
            Repository = repository;
        }

        /// <summary>
        /// Should apply sorting if needed.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="input">The input.</param>
        protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, TGetAllInput input)
        {
            //Try to sort query if available
            var sortInput = input as ISortQuery;
            if (sortInput != null)
            {
                if (!string.IsNullOrEmpty(sortInput.Sort))
                {
                    return query.OrderBy(sortInput.Sort);
                }
            }

            //IQueryable.Task requires sorting, so we should sort if Take will be used.
            if (input is ILimitQuery)
            {
                return query.OrderByDescending(e => e.Id);
            }

            //No sorting
            return query;
        }

        /// <summary>
        /// Should apply paging if needed.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="input">The input.</param>
        protected virtual IQueryable<TEntity> ApplyPaging(IQueryable<TEntity> query, TGetAllInput input)
        {
            //Try to use paging if available
            var pagedInput = input as IPageQuery;
            if (pagedInput != null)
            {
                return query.Skip(pagedInput.Skip).Take(pagedInput.Limit);
            }

            //Try to limit query result if available
            var limitedInput = input as ILimitQuery;
            if (limitedInput != null)
            {
                return query.Take(limitedInput.Limit);
            }

            //No paging
            return query;
        }

        /// <summary>
        /// This method should create <see cref="IQueryable{TEntity}"/> based on given input.
        /// It should filter query if needed, but should not do sorting or paging.
        /// Sorting should be done in <see cref="ApplySorting"/> and paging should be done in <see cref="ApplyPaging"/>
        /// methods.
        /// </summary>
        /// <param name="input">The input.</param>
        protected virtual IQueryable<TEntity> CreateFilteredQuery(TGetAllInput input)
        {
            return Repository.GetList();
        }


        /// <summary>
        /// This method should create <see cref="IQueryable{TEntity}"/> based on given input.
        /// It should filter query if needed, but should not do sorting or paging.
        /// Sorting should be done in <see cref="ApplySorting"/> and paging should be done in <see cref="ApplyPaging"/>
        /// methods.
        /// </summary>
        /// <param name="input">The input.</param>
        protected virtual Task<IQueryable<TEntity>> CreateFilteredQueryAsync(TGetAllInput input)
        {
            return Repository.GetListAsync();
        }

        /// <summary>
        /// Maps <see cref="TEntity"/> to <see cref="TEntityDto"/>.
        /// It uses <see cref="IObjectMapper"/> by default.
        /// It can be overrided for custom mapping.
        /// </summary>
        protected virtual TEntityDto MapToEntityDto(TEntity entity)
        {
            return ObjectMapper.Map<TEntityDto>(entity);
        }

        /// <summary>
        /// Maps <see cref="TEntityDto"/> to <see cref="TEntity"/> to create a new entity.
        /// It uses <see cref="IObjectMapper"/> by default.
        /// It can be overrided for custom mapping.
        /// </summary>
        protected virtual TEntity MapToEntity(TCreateInput createInput)
        {
            return ObjectMapper.Map<TEntity>(createInput);
        }

        /// <summary>
        /// Maps <see cref="TUpdateInput"/> to <see cref="TEntity"/> to update the entity.
        /// It uses <see cref="IObjectMapper"/> by default.
        /// It can be overrided for custom mapping.
        /// </summary>
        protected virtual void MapToEntity(TUpdateInput updateInput, TEntity entity)
        {
            ObjectMapper.Map(updateInput, entity);
        }

        protected virtual void CheckGetPermission()
        {
            CheckPermission(GetPermissionName);
        }

        protected virtual void CheckGetAllPermission()
        {
            CheckPermission(GetAllPermissionName);
        }

        protected virtual void CheckCreatePermission()
        {
            CheckPermission(CreatePermissionName);
        }

        protected virtual void CheckUpdatePermission()
        {
            CheckPermission(UpdatePermissionName);
        }

        protected virtual void CheckDeletePermission()
        {
            CheckPermission(DeletePermissionName);
        }
    }
}
