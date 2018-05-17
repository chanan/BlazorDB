using System;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorDB.Storage
{
    internal class StorageManager : IStorageManager
    {
        private StorageManagerSave _storageManagerSave = new StorageManagerSave();
        private StorageManagerLoad _storageManagerLoad = new StorageManagerLoad();

        public int SaveContextToLocalStorage(StorageContext context)
        {
            return _storageManagerSave.SaveContextToLocalStorage(context);
        }

        public void LoadContextFromStorageOrCreateNew(IServiceCollection serviceCollection, Type contextType)
        {
            _storageManagerLoad.LoadContextFromStorageOrCreateNew(serviceCollection, contextType);
        }
    }
}
