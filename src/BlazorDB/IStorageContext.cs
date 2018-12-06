using System.Threading.Tasks;

namespace BlazorDB
{
    public interface IStorageContext
    {
        Task<int> SaveChanges();
        Task LogToConsole();
    }
}
