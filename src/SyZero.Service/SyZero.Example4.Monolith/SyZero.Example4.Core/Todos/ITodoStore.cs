using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyZero.Example4.Core.Todos
{
    public interface ITodoStore
    {
        Task<IReadOnlyList<TodoItem>> GetListAsync();

        Task<TodoItem> GetAsync(long id);

        Task<TodoItem> AddAsync(TodoItem item);

        Task<TodoItem> CompleteAsync(long id);
    }
}
