using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyZero;
using SyZero.Dependency;
using SyZero.Example4.Core.Todos;
using SyZero.Example4.IApplication.Todos;
using SyZero.Example4.IApplication.Todos.Dto;

namespace SyZero.Example4.Application.Todos
{
    public class TodoAppService : ITodoAppService, IScopedDependency
    {
        private readonly ITodoStore _todoStore;

        public TodoAppService(ITodoStore todoStore)
        {
            _todoStore = todoStore;
        }

        public async Task<IReadOnlyList<TodoItemDto>> GetListAsync()
        {
            var items = await _todoStore.GetListAsync();
            return items.Select(ToDto).ToList();
        }

        public async Task<TodoItemDto> GetAsync(long id)
        {
            return ToDto(await _todoStore.GetAsync(id));
        }

        public async Task<TodoItemDto> CreateAsync(CreateTodoItemDto input)
        {
            var item = await _todoStore.AddAsync(new TodoItem
            {
                Title = input.Title,
                IsCompleted = false
            });

            return ToDto(item);
        }

        public async Task<TodoItemDto> CompleteAsync(CompleteTodoItemDto input)
        {
            return ToDto(await _todoStore.CompleteAsync(input.Id));
        }

        private static TodoItemDto ToDto(TodoItem item)
        {
            return new TodoItemDto
            {
                Id = item.Id,
                Title = item.Title,
                IsCompleted = item.IsCompleted,
                CreatedAt = item.CreatedAt,
                CompletedAt = item.CompletedAt
            };
        }
    }
}
