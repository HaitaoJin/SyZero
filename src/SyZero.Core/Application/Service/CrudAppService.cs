
using System.Collections.Generic;
using System.Linq;
using SyZero.Application.Service.Dto;
using SyZero.Domain.Entities;
using SyZero.Domain.Repository;

namespace SyZero.Application.Service
{


    public abstract class CrudAppService<TEntity, TEntityDto>
        : CrudAppService<TEntity, TEntityDto, PageAndSortQueryDto>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
    {
        protected CrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class CrudAppService<TEntity, TEntityDto, TGetAllInput>
        : CrudAppService<TEntity, TEntityDto, TGetAllInput, TEntityDto, TEntityDto>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
    {
        protected CrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class CrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput>
        : CrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TCreateInput>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
        where TCreateInput : IEntityDto
    {
        protected CrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class CrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput>
        : CrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput, EntityDto>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
        where TUpdateInput : IEntityDto
    {
        protected CrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class CrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput, TGetInput>
    : CrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput, TGetInput, EntityDto>
        where TEntity : class, IEntity
        where TEntityDto : IEntityDto
        where TUpdateInput : IEntityDto
        where TGetInput : IEntityDto
    {
        protected CrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }

    public abstract class CrudAppService<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput, TGetInput, TDeleteInput>
       : CrudAppServiceBase<TEntity, TEntityDto, TGetAllInput, TCreateInput, TUpdateInput>,
        ICrudAppService<TEntityDto, TGetAllInput, TCreateInput, TUpdateInput, TGetInput, TDeleteInput>
           where TEntity : class, IEntity
           where TEntityDto : IEntityDto
           where TUpdateInput : IEntityDto
           where TGetInput : IEntityDto
           where TDeleteInput : IEntityDto
    {
        protected CrudAppService(IRepository<TEntity> repository)
            : base(repository)
        {

        }

        public virtual TEntityDto Get(TGetInput input)
        {
            CheckGetPermission();

            var entity = GetEntityById(input.Id);
            return MapToEntityDto(entity);
        }

        public virtual PageResultDto<TEntityDto> GetAll(TGetAllInput input)
        {
            CheckGetAllPermission();

            var query = CreateFilteredQuery(input);

            var totalCount = query.Count();

            query = ApplySorting(query, input);
            query = ApplyPaging(query, input);

            var entities = query.ToList();
            // ObjectMapper.Map<List<TEntityDto> >(entities);
            return new PageResultDto<TEntityDto>(
                totalCount,
                ObjectMapper.Map<List<TEntityDto>>(entities)
            );
        }

        public virtual TEntityDto Create(TCreateInput input)
        {
            CheckCreatePermission();

            var entity = MapToEntity(input);

            Repository.Add(entity);

            return MapToEntityDto(entity);
        }

        public virtual TEntityDto Update(TUpdateInput input)
        {
            CheckUpdatePermission();

            var entity = GetEntityById(input.Id);

            MapToEntity(input, entity);

            return MapToEntityDto(entity);
        }

        public virtual bool Delete(TDeleteInput input)
        {
            CheckDeletePermission();

            return Repository.Delete(input.Id) > 0;
        }

        protected virtual TEntity GetEntityById(long id)
        {
            return Repository.GetModel(id);
        }
    }
}
