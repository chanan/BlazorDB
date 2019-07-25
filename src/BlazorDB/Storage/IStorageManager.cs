using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    public interface IStorageManager
    {
        Task<int> SaveContextToLocalStorage(StorageContext context);
        Task LoadContextFromLocalStorage(StorageContext context);
    }
}
