using System;
using System.Reflection;
using Microsoft.AspNetCore.Blazor;

namespace BlazorDB
{
    public class StorageContext : IStorageContext
    {
        public void SaveChanges()
        {
            var type = GetType();
            Logger.ContextSaved(type);
            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                {
                    var storageSetValue = prop.GetValue(this);
                    var modelType = prop.PropertyType.GetGenericArguments()[0];
                    var storageTableName = Util.GetStorageTableName(type, modelType);
                    BlazorDBInterop.SetItem(storageTableName, JsonUtil.Serialize(storageSetValue), false);
                    var count = GetListCount(prop);
                    Logger.StorageSetSaved(modelType, count);
                }
            }
            Logger.EndGroup();
        }

        private int GetListCount(PropertyInfo prop)
        {
            var list = prop.GetValue(this);
            var countProp = list.GetType().GetProperty("Count");
            return (int)countProp.GetValue(list);
        }
    }
}
