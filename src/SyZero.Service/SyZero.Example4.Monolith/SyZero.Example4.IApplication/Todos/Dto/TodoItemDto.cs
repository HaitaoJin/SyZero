using System;

namespace SyZero.Example4.IApplication.Todos.Dto
{
    public class TodoItemDto
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
