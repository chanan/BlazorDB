using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal interface IStorageManagerSave
    {
        Task<int> SaveContextToLocalStorage(StorageContext context);
    }
}