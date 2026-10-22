

using SyZero.Application.Service.Dto;

namespace SyZero.Application.Service
{

    public interface ICrudAppService<TEntityDto>
        : ICrudAppService<TEntityDto, PageAndSortQueryDto>
        where TEntityDto : IEntityDto
    {

    }

    public interface ICrudAppService<TEntityDto, in TGetAllInput>
        : ICrudAppService<TEntityDto, TGetAllInput, TEntityDto, TEntityDto>
        where TEntityDto : IEntityDto
    {

    }

    public interface ICrudAppService<TEntityDto, in TGetAllInput, in TCreateInput>
        : ICrudAppService<TEntityDto, TGetAllInput, TCreateInput, TCreateInput>
        where TEntityDto : IEntityDto
        where TCreateInput : IEntityDto
    {

    }

    public interface ICrudAppService<TEntityDto, in TGetAllInput, in TCreateInput, in TUpdateInput>
        : ICrudAppService<TEntityDto, TGetAllInput, TCreateInput, TUpdateInput, EntityDto>
        where TEntityDto : IEntityDto
        where TUpdateInput : IEntityDto
    {

    }

    public interface ICrudAppService<TEntityDto, in TGetAllInput, in TCreateInput, in TUpdateInput, in TGetInput>
        : ICrudAppService<TEntityDto, TGetAllInput, TCreateInput, TUpdateInput, TGetInput, EntityDto>
        where TEntityDto : IEntityDto
        where TUpdateInput : IEntityDto
        where TGetInput : IEntityDto
    {

    }

    public interface ICrudAppService<TEntityDto, in TGetAllInput, in TCreateInput, in TUpdateInput, in TGetInput, in TDeleteInput>
        : IApplicationService
        where TEntityDto : IEntityDto
        where TUpdateInput : IEntityDto
        where TGetInput : IEntityDto
        where TDeleteInput : IEntityDto
    {
        TEntityDto Get(TGetInput input);

        PageResultDto<TEntityDto> GetAll(TGetAllInput input);

        TEntityDto Create(TCreateInput input);

        TEntityDto Update(TUpdateInput input);

        bool Delete(TDeleteInput input);
    }
}
