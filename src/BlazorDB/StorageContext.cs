using BlazorDB.Storage;

namespace BlazorDB
{
    public class StorageContext : IStorageContext
    {
        private IStorageManager StorageManager { get; set; } = new StorageManager(); // Dep injection not working right now

        public void LogToConsole()
        {
            Logger.StartContextType(GetType(), false);
            var storageSets = StorageManagerUtil.GetStorageSets(GetType());
            foreach (var prop in storageSets)
            {
                var storageSet = prop.GetValue(this);
                var method = storageSet.GetType().GetMethod("LogToConsole");
                method.Invoke(storageSet, new object[]{});
            }
            Logger.EndGroup();
        }

        public int SaveChanges()
        {
            return StorageManager.SaveContextToLocalStorage(this);
        }
    }
}