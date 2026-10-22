using SyZero.Application.Attributes;
using SyZero.Application.Service;

namespace SyZero.Example3.IApplication
{
    [DynamicApi]
    public interface IApplicationServiceBase : IApplicationService, IDynamicApi
    {
    }
}
