using Microsoft.Extensions.DependencyInjection;
using System;

namespace BlazorDB.Storage
{
    public interface IStorageManager
    {
        int SaveContextToLocalStorage(StorageContext context);
        void LoadContextFromStorageOrCreateNew(IServiceCollection serviceCollection, Type contextType);
    }
}
