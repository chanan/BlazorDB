using System;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal class StorageManager : IStorageManager
    {
        private readonly IStorageManagerLoad _storageManagerLoad;
        private readonly IStorageManagerSave _storageManagerSave;

        public StorageManager(IStorageManagerSave storageManagerSave, IStorageManagerLoad storageManagerLoad)
        {
            _storageManagerSave = storageManagerSave;
            _storageManagerLoad = storageManagerLoad;

        }

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