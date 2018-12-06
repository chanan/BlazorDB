using System;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal class StorageManager : IStorageManager
    {
        private readonly StorageManagerLoad _storageManagerLoad = new StorageManagerLoad();
        private readonly StorageManagerSave _storageManagerSave = new StorageManagerSave();

        public Task<int> SaveContextToLocalStorage(StorageContext context)
        {
            return _storageManagerSave.SaveContextToLocalStorage(context);
        }

        public Task LoadContextFromLocalStorage(StorageContext context)
        {
            return _storageManagerLoad.LoadContextFromLocalStorage(context);
        }
    }
}