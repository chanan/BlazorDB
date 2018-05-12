using BlazorDB.Storage;

namespace BlazorDB
{
    public class StorageContext : IStorageContext
    {
        private IStorageManager StorageManager { get; set; } = new StorageManager(); // Dep injection not working right now

        public int SaveChanges()
        {
            return StorageManager.SaveContextToLocalStorage(this);
        }
    }
}