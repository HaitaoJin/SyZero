using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Example4.Core.Todos
{
    public class InMemoryTodoStore : ITodoStore, ISingletonDependency
    {
        private readonly ConcurrentDictionary<long, TodoItem> _items = new ConcurrentDictionary<long, TodoItem>();
        private long _currentId;

        public InMemoryTodoStore()
        {
            AddSeed("梳理单体服务分层结构");
            AddSeed("通过 DynamicWebApi 暴露应用服务");
        }

        public Task<IReadOnlyList<TodoItem>> GetListAsync()
        {
            IReadOnlyList<TodoItem> items = _items.Values
                .OrderBy(item => item.Id)
                .ToList();

            return Task.FromResult(items);
        }

        public Task<TodoItem> GetAsync(long id)
        {
            if (!_items.TryGetValue(id, out var item))
            {
                throw new InvalidOperationException($"待办事项不存在: {id}");
            }

            return Task.FromResult(item);
        }

        public Task<TodoItem> AddAsync(TodoItem item)
        {
            item.Id = Interlocked.Increment(ref _currentId);
            item.CreatedAt = DateTime.Now;
            _items[item.Id] = item;
            return Task.FromResult(item);
        }

        public Task<TodoItem> CompleteAsync(long id)
        {
            if (!_items.TryGetValue(id, out var item))
            {
                throw new InvalidOperationException($"待办事项不存在: {id}");
            }

            item.IsCompleted = true;
            item.CompletedAt = DateTime.Now;
            return Task.FromResult(item);
        }

        private void AddSeed(string title)
        {
            AddAsync(new TodoItem
            {
                Title = title,
                IsCompleted = false
            }).GetAwaiter().GetResult();
        }
    }
}
