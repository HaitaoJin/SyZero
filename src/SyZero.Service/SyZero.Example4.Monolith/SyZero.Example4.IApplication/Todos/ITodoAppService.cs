using System.Collections.Generic;
using System.Threading.Tasks;
using SyZero.Application.Routing;
using SyZero.Example4.IApplication.Todos.Dto;

namespace SyZero.Example4.IApplication.Todos
{
    public interface ITodoAppService : IApplicationServiceBase
    {
        [Get("List")]
        Task<IReadOnlyList<TodoItemDto>> GetListAsync();

        [Get("Get")]
        Task<TodoItemDto> GetAsync(long id);

        [Post("Create")]
        Task<TodoItemDto> CreateAsync(CreateTodoItemDto input);

        [Post("Complete")]
        Task<TodoItemDto> CompleteAsync(CompleteTodoItemDto input);
    }
}
