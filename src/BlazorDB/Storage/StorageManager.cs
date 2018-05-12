using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Blazor;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorDB.Storage
{
    internal class StorageManager : IStorageManager
    {
        private static readonly Type storageContext = typeof(StorageContext);
        private static readonly Type genericStorageSetType = typeof(StorageSet<>);
        private static readonly Type genericListType = typeof(List<>);
        private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public int SaveContextToLocalStorage(StorageContext context)
        {
            int total = 0;
            var contextType = context.GetType();
            //Logger.ContextSaved(contextType);
            var storageSets = (from prop in contextType.GetProperties()
                            where prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>)
                            select prop).ToList();
            foreach (var prop in storageSets)
            {
                var storageSetValue = prop.GetValue(context);
                var modelType = prop.PropertyType.GetGenericArguments()[0];
                var storageTableName = Util.GetStorageTableName(contextType, modelType);
                var guids = SaveModels(storageSetValue, modelType, storageTableName, storageSets);
                total += guids.Count;
                var oldMetadata = LoadMetadata(storageTableName);
                SaveMetadata(storageTableName, guids, contextType, modelType);
                if (oldMetadata != null) DeleteOldModelsFromStorage(oldMetadata, storageTableName);
                Logger.StorageSetSaved(modelType, guids.Count);
            }
            //Logger.EndGroup();
            return total;
        }

        public void LoadContextFromStorageOrCreateNew(IServiceCollection serviceCollection, Type contextType)
        {
            var context = Activator.CreateInstance(contextType);
            Logger.StartContextType(contextType);
            foreach (var prop in contextType.GetProperties())
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                {
                    var modelType = prop.PropertyType.GetGenericArguments()[0];
                    Logger.LoadModelInContext(modelType);
                    var storageSetType = genericStorageSetType.MakeGenericType(modelType);
                    var storageTableName = Util.GetStorageTableName(contextType, modelType);
                    var metadata = LoadMetadata(storageTableName);
                    var storageSet = metadata != null ? LoadStorageSet(metadata, storageTableName, storageSetType, contextType, modelType) : CreateNewStorageSet(storageSetType);
                    prop.SetValue(context, storageSet);
                }
            }
            RegisterContext(serviceCollection, contextType, context);
            Logger.EndGroup();
        }

        private void DeleteOldModelsFromStorage(Metadata metadata, string storageTableName)
        {
            foreach (var guid in metadata.Guids)
            {
                var name = $"{storageTableName}-{guid}";
                BlazorDBInterop.RemoveItem(name, false);
            }
        }

        private void SaveMetadata(string storageTableName, List<Guid> guids, Type context, Type modelType)
        {
            var metadata = new Metadata { Guids = guids, ContextName = Util.GetFullyQualifiedTypeName(context), ModelName = Util.GetFullyQualifiedTypeName(modelType) };
            var name = $"{storageTableName}-metadata";
            BlazorDBInterop.SetItem(name, JsonUtil.Serialize(metadata), false);

        }

        private List<Guid> SaveModels(object storageSetValue, Type modelType, string storageTableName, List<PropertyInfo> storageSets)
        {
            var guids = new List<Guid>();
            var storageSetType = genericStorageSetType.MakeGenericType(modelType);
            var method = storageSetType.GetMethod("GetEnumerator");
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

        private string ScanModelForAssociations(object model, List<PropertyInfo> storageSets, string serializedModel)
        {
            var result = serializedModel;
            foreach (var prop in model.GetType().GetProperties())
            {
                if(IsInContext(storageSets, prop) && prop.GetValue(model) != null)
                {
                    Console.WriteLine("Found Association");
                    var associatedModel = prop.GetValue(model);
                    var IdProp = associatedModel.GetType().GetProperty("Id"); //TODO: Handle missing Id prop
                    var id = IdProp.GetValue(associatedModel);
                    Console.WriteLine("Found Association: {0}, Id: {1}", prop.Name, id);
                    result = ReplaceAssociation(result, prop.Name, id);
                }
            }
            return result;
        }

        //TODO: A better way to do this would be to use dynamic, which right now doesn't work in Blazor
        private string ReplaceAssociation(string result, string name, object id)
        {
            var stringToFind = $"\"{name}\":{{";
            var start = result.IndexOf(stringToFind);
            var foundFirst = false;
            var arr = result.ToCharArray();
            int count = 0, open = 0, close = 0;
            for(int i = start; i < arr.Length; i++)
            {
                var ch = arr[i];
                if (ch == '{') {
                    count++;
                    foundFirst = true;
                    open = i;
                }
                if (ch == '}') count--;
                if (foundFirst && count == 0)
                {
                    close = i;
                    break;
                }
            }
            result = ReplaceString(result, open, close + 1, id);
            return result;
        }

        private string ReplaceString(string result, int open, int close, object id)
        {
            var start = result.Substring(0, open);
            var end = result.Substring(close);
            return start + id + end;
        }

        private bool IsInContext(List<PropertyInfo> storageSets, PropertyInfo prop)
        {
            var query = from p in storageSets
                        where p.PropertyType.GetGenericArguments()[0] == prop.PropertyType
                        select p;
            return query.SingleOrDefault() !=null;
        }

        private Metadata LoadMetadata(string storageTableName)
        {
            var name = $"{storageTableName}-metadata";
            var value = BlazorDBInterop.GetItem(name, false);
            return value != null ? JsonUtil.Deserialize<Metadata>(value) : null;
        }

        private int GetListCount(StorageContext context, PropertyInfo prop)
        {
            var list = prop.GetValue(context);
            var countProp = list.GetType().GetProperty("Count");
            return (int)countProp.GetValue(list);
        }

        private object CreateNewStorageSet(Type storageSetType)
        {
            return Activator.CreateInstance(storageSetType);
        }

        private object LoadStorageSet(Metadata metadata, string storageTableName, Type storageSetType, Type contextType, Type modelType)
        {
            var instance = CreateNewStorageSet(storageSetType);
            var prop = storageSetType.GetProperty("StorageContextTypeName", flags);
            prop.SetValue(instance, Util.GetFullyQualifiedTypeName(contextType));
            var listGenericType = genericListType.MakeGenericType(modelType);
            var list = Activator.CreateInstance(listGenericType);
            foreach (var guid in metadata.Guids)
            {
                var name = $"{storageTableName}-{guid}";
                var value = BlazorDBInterop.GetItem(name, false);
                var model = Deserialize(modelType, value);
                var addMethod = listGenericType.GetMethod("Add");
                addMethod.Invoke(list, new object[] { model });
            }
            return SetList(instance, list);
        }

        private object SetList(object instance, object list)
        {
            var prop = instance.GetType().GetProperty("List", flags);
            prop.SetValue(instance, list);
            return instance;
        }

        private object Deserialize(Type modelType, string value)
        {
            var method = typeof(JsonWrapper).GetMethod("Deserialize");
            var genericMethod = method.MakeGenericMethod(modelType);
            var model = genericMethod.Invoke(new JsonWrapper(), new object[] { value });
            return model;
        }

        private void RegisterContext(IServiceCollection serviceCollection, Type type, object context)
        {
            serviceCollection.AddSingleton(
                serviceType: type,
                implementationInstance: context);
        }
    }

    class JsonWrapper
    {
        public T Deserialize<T>(string value) => JsonUtil.Deserialize<T>(value);
    }
}
