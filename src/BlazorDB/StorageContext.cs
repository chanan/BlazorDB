using Microsoft.AspNetCore.Blazor;

namespace BlazorDB
{
    public class StorageContext : IStorageContext
    {
        public void SaveChanges()
        {
            var type = GetType();
            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                {
                    var storageSetValue = prop.GetValue(this);
                    var modelType = prop.PropertyType.GetGenericArguments()[0];
                    var storageTableName = Util.GetStorageTableName(type, modelType);
                    BlazorDBInterop.SetItem(storageTableName, JsonUtil.Serialize(storageSetValue), false);
                }
            }
        }
    }
}
