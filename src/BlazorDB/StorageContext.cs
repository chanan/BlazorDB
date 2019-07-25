using BlazorDB.Storage;
using System.Threading.Tasks;

namespace BlazorDB
{
    public class StorageContext : IStorageContext
    {
        protected IStorageManager StorageManager { get; set; }
        protected IBlazorDBLogger Logger { get; set; }
        protected IStorageManagerUtil StorageManagerUtil { get; set; }
        private bool _initalized = false;

        public async Task LogToConsole()
        {
            await Logger.StartContextType(GetType(), false);
            System.Collections.Generic.List<System.Reflection.PropertyInfo> storageSets = StorageManagerUtil.GetStorageSets(GetType());
            foreach (System.Reflection.PropertyInfo prop in storageSets)
            {
                object storageSet = prop.GetValue(this);
                System.Reflection.MethodInfo method = storageSet.GetType().GetMethod("LogToConsole");
                method.Invoke(storageSet, new object[] { });
            }
            Logger.EndGroup();
        }

        public Task<int> SaveChanges()
        {
            return StorageManager.SaveContextToLocalStorage(this);
        }

        public Task Initialize()
        {
            if (_initalized)
            {
                return Task.CompletedTask;
            }

            _initalized = true;
            return StorageManager.LoadContextFromLocalStorage(this);
        }
    }
}