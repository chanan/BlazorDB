using BlazorDB.Storage;

namespace BlazorDB
{
    public class StorageContext : IStorageContext
    {
        public int SaveChanges()
        {
            return StorageManager.SaveContextToLocalStorage(this);
        }
    }
}
