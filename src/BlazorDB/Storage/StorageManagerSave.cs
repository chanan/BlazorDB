using Microsoft.AspNetCore.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BlazorDB.Storage
{
    internal class StorageManagerSave
    {
        public int SaveContextToLocalStorage(StorageContext context)
        {
            int total = 0;
            var contextType = context.GetType();
            Logger.ContextSaved(contextType);
            var storageSets = StorageManagerUtil.GetStorageSets(contextType);
            total = SaveStorageSets(context, total, contextType, storageSets);
            Logger.EndGroup();
            return total;
        }

        private int SaveStorageSets(StorageContext context, int total, Type contextType, List<PropertyInfo> storageSets)
        {
            foreach (var prop in storageSets)
            {
                var storageSetValue = prop.GetValue(context);
                var modelType = prop.PropertyType.GetGenericArguments()[0];
                var storageTableName = Util.GetStorageTableName(contextType, modelType);
                var guids = SaveModels(storageSetValue, modelType, storageTableName, storageSets);
                total += guids.Count;
                var oldMetadata = StorageManagerUtil.LoadMetadata(storageTableName);
                SaveMetadata(storageTableName, guids, contextType, modelType);
                if (oldMetadata != null) DeleteOldModelsFromStorage(oldMetadata, storageTableName);
                Logger.StorageSetSaved(modelType, guids.Count);
            }
            return total;
        }

        private void SaveMetadata(string storageTableName, List<Guid> guids, Type context, Type modelType)
        {
            var metadata = new Metadata { Guids = guids, ContextName = Util.GetFullyQualifiedTypeName(context), ModelName = Util.GetFullyQualifiedTypeName(modelType) };
            var name = $"{storageTableName}-{StorageManagerUtil.METADATA}";
            BlazorDBInterop.SetItem(name, JsonUtil.Serialize(metadata), false);
        }

        private List<Guid> SaveModels(object storageSetValue, Type modelType, string storageTableName, List<PropertyInfo> storageSets)
        {
            var guids = new List<Guid>();
            var storageSetType = StorageManagerUtil.genericStorageSetType.MakeGenericType(modelType);
            var method = storageSetType.GetMethod(StorageManagerUtil.GET_ENUMERATOR);
            var enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                var guid = Guid.NewGuid();
                guids.Add(guid);
                var model = enumerator.Current;
                var name = $"{storageTableName}-{guid}";
                var serializedModel = ScanModelForAssociations(model, storageSets, JsonUtil.Serialize(model));
                BlazorDBInterop.SetItem(name, serializedModel, false);
            }
            return guids;
        }

        private void DeleteOldModelsFromStorage(Metadata metadata, string storageTableName)
        {
            foreach (var guid in metadata.Guids)
            {
                var name = $"{storageTableName}-{guid}";
                BlazorDBInterop.RemoveItem(name, false);
            }
        }

        private string ScanModelForAssociations(object model, List<PropertyInfo> storageSets, string serializedModel)
        {
            var result = serializedModel;
            foreach (var prop in model.GetType().GetProperties())
            {
                if (prop.GetValue(model) != null && StorageManagerUtil.IsInContext(storageSets, prop))
                {
                    var associatedModel = prop.GetValue(model);
                    var idProp = associatedModel.GetType().GetProperty(StorageManagerUtil.ID); //TODO: Handle missing Id prop
                    var id = Convert.ToString(idProp.GetValue(associatedModel));
                    var serializedItem = JsonUtil.Serialize(model);
                    result = ReplaceModelWithId(result, serializedItem, id);
                }
                if (prop.GetValue(model) != null && StorageManagerUtil.IsListInContext(storageSets, prop))
                {
                    var modelList = (IEnumerable)prop.GetValue(model);
                    foreach(var item in modelList)
                    {
                        var idProp = item.GetType().GetProperty(StorageManagerUtil.ID); //TODO: Handle missing Id prop
                        var id = Convert.ToString(idProp.GetValue(item));
                        var serializedItem = JsonUtil.Serialize(item);
                        result = ReplaceModelWithId(result, serializedItem, id);
                    }

                }
            }
            return result;
        }

        private string ReplaceModelWithId(string result, string serializedItem, string id)
        {
            return result.Replace(serializedItem, id);
        }
    }
}
