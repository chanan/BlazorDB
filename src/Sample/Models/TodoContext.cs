using BlazorDB;

namespace Sample.Models
{
    public class TodoContext : StorageContext
    {
        public StorageSet<TodoItem> Todos { get; set; }
    }
}
