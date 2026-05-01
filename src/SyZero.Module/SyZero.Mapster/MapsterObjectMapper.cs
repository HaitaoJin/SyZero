using System;
using Mapster;
using MapsterMapper;

namespace SyZero.Mapster
{
    /// <summary>
    /// 基于 Mapster 的 <see cref="SyZero.ObjectMapper.IObjectMapper"/> 实现。
    /// 通过 <see cref="IMapper"/> 转发调用，使上层业务代码与具体映射库解耦。
    /// </summary>
    public class MapsterObjectMapper : SyZero.ObjectMapper.IObjectMapper
    {
        private readonly IMapper _mapper;

        public MapsterObjectMapper(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public TDestination Map<TDestination>(object source)
        {
            return _mapper.Map<TDestination>(source);
        }

        /// <inheritdoc />
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            return _mapper.Map(source, destination);
        }
    }
}
