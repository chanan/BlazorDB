using System.Reflection;
using Microsoft.AspNetCore.Blazor;

namespace BlazorDB.Storage
{
    internal static class StorageManager
    {
        public static int SaveContextToLocalStorage(StorageContext context)
        {
            int total = 0;
            var type = context.GetType();
            Logger.ContextSaved(type);
            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                {
                    var storageSetValue = prop.GetValue(context);
                    var modelType = prop.PropertyType.GetGenericArguments()[0];
                    var storageTableName = Util.GetStorageTableName(type, modelType);
                    BlazorDBInterop.SetItem(storageTableName, JsonUtil.Serialize(storageSetValue), false);
                    var count = GetListCount(context, prop);
                    Logger.StorageSetSaved(modelType, count);
                    total += count;
                }
            }
            Logger.EndGroup();
            return total;
        }

        private static int GetListCount(StorageContext context, PropertyInfo prop)
        {
            var list = prop.GetValue(context);
            var countProp = list.GetType().GetProperty("Count");
            return (int)countProp.GetValue(list);
        }
    }
}
