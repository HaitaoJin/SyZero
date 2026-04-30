
using System.Linq;
using System.Threading.Tasks;
using SyZero.Application.Service.Dto;
using SyZero.Domain.Entities;
using SyZero.Domain.Repository;

namespace SyZero.Application.Service
{
    public abstract class AsyncCrudAppService<TEntity, TEntityDto>
        : AsyncCrudAppService<TEntity, TEntityDto, PageAndSortQueryDto>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
    {
        protected AsyncCrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class AsyncCrudAppService<TEntity, TEntityDto, TGetAllInput>
        : AsyncCrudAppService<TEntity, TEntityDto, TGetAllInput, TEntityDto>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
    {
        protected AsyncCrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class AsyncCrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput>
    : AsyncCrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TEntityDto>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
        where TCreateInput : IEntityDto
    {
        protected AsyncCrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class AsyncCrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput>
       : CrudAppServiceBase<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput>,
        IAsyncCrudAppService<TEntityDto, TGetAllInput, TCreateInput, TUpdateInput>
           where TEntity : class, IEntity
           where TEntityDto : IEntityDto
           where TUpdateInput : IEntityDto
    {
        protected AsyncCrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
        public virtual async Task<TEntityDto> Get(long id)
        {
            CheckGetPermission();

            var entity = await GetEntityByIdAsync(id);
            return MapToEntityDto(entity);
        }

        public virtual async Task<PageResultDto<TEntityDto>> List(TGetAllInput input)
        {
            CheckGetAllPermission();

            var query = await CreateFilteredQueryAsync(input);

            var totalCount = query.Count();

            query = ApplySorting(query, input);
            query = ApplyPaging(query, input);

            var entities = query.ToList();

            return new PageResultDto<TEntityDto>(
                totalCount,
                entities.Select(MapToEntityDto).ToList()
            );
        }

        public virtual async Task<TEntityDto> Create(TCreateInput input)
        {
            CheckCreatePermission();

            var entity = MapToEntity(input);

            await Repository.AddAsync(entity);

            return MapToEntityDto(entity);
        }

        public virtual async Task<TEntityDto> Update(long id, TUpdateInput input)
        {
            CheckUpdatePermission();

            var entity = await GetEntityByIdAsync(id);

            MapToEntity(input, entity);

            await Repository.UpdateAsync(entity);

            return MapToEntityDto(entity);
        }

        public virtual async Task<bool> Delete(long id)
        {
            CheckDeletePermission();
            await Repository.DeleteAsync(id);
            return true;
        }

        protected virtual async Task<TEntity> GetEntityByIdAsync(long id)
        {
            return await Repository.GetModelAsync(id);
        }
    }
}
