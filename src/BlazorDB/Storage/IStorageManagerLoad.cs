using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal interface IStorageManagerLoad
    {
        Task LoadContextFromLocalStorage(StorageContext context);
    }
}