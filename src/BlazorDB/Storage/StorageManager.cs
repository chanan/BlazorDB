using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Blazor;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorDB.Storage
{
    internal static class StorageManager
    {
        private static readonly Type storageContext = typeof(StorageContext);
        private static readonly Type genericStorageSetType = typeof(StorageSet<>);
        private static readonly Type genericListType = typeof(List<>);
        private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static int SaveContextToLocalStorage(StorageContext context)
        {
            int total = 0;
            var contextType = context.GetType();
            Logger.ContextSaved(contextType);
            foreach (var prop in contextType.GetProperties())
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                {
                    var storageSetValue = prop.GetValue(context);
                    var modelType = prop.PropertyType.GetGenericArguments()[0];
                    var storageTableName = Util.GetStorageTableName(contextType, modelType);
                    var guids = SaveModels(storageSetValue, modelType, storageTableName);
                    total += guids.Count;
                    var oldMetadata = LoadMetadata(storageTableName);
                    SaveMetadata(storageTableName, guids, contextType, modelType);
                    if (oldMetadata != null) DeleteOldModelsFromStorage(oldMetadata, storageTableName);
                    Logger.StorageSetSaved(modelType, guids.Count);
                }
            }
            Logger.EndGroup();
            return total;
        }

        public static void LoadContextFromStorageOrCreateNew(IServiceCollection serviceCollection, Type contextType)
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

        private static void DeleteOldModelsFromStorage(Metadata metadata, string storageTableName)
        {
            foreach (var guid in metadata.Guids)
            {
                var name = $"{storageTableName}-{guid}";
                BlazorDBInterop.RemoveItem(name, false);
            }
        }

        private static void SaveMetadata(string storageTableName, List<Guid> guids, Type context, Type modelType)
        {
            var metadata = new Metadata { Guids = guids, ContextName = Util.GetFullyQualifiedTypeName(context), ModelName = Util.GetFullyQualifiedTypeName(modelType) };
            var name = $"{storageTableName}-metadata";
            BlazorDBInterop.SetItem(name, JsonUtil.Serialize(metadata), false);

        }

        private static List<Guid> SaveModels(object storageSetValue, Type modelType, string storageTableName)
        {
            var guids = new List<Guid>();
            var storageSetType = genericStorageSetType.MakeGenericType(modelType);
            var method = storageSetType.GetMethod("GetEnumerator");
            var enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                var guid = Guid.NewGuid();
                guids.Add(guid);
                var item = enumerator.Current;
                var name = $"{storageTableName}-{guid}";
                BlazorDBInterop.SetItem(name, JsonUtil.Serialize(item), false);
            }
            return guids;
        }

        private static Metadata LoadMetadata(string storageTableName)
        {
            var name = $"{storageTableName}-metadata";
            var value = BlazorDBInterop.GetItem(name, false);
            return value != null ? JsonUtil.Deserialize<Metadata>(value) : null;
        }

        private static int GetListCount(StorageContext context, PropertyInfo prop)
        {
            var list = prop.GetValue(context);
            var countProp = list.GetType().GetProperty("Count");
            return (int)countProp.GetValue(list);
        }

        private static object CreateNewStorageSet(Type storageSetType)
        {
            return Activator.CreateInstance(storageSetType);
        }

        private static object LoadStorageSet(Metadata metadata, string storageTableName, Type storageSetType, Type contextType, Type modelType)
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

        private static object SetList(object instance, object list)
        {
            var prop = instance.GetType().GetProperty("List", flags);
            prop.SetValue(instance, list);
            return instance;
        }

        private static object Deserialize(Type modelType, string value)
        {
            var method = typeof(JsonWrapper).GetMethod("Deserialize");
            var genericMethod = method.MakeGenericMethod(modelType);
            var model = genericMethod.Invoke(new JsonWrapper(), new object[] { value });
            return model;
        }

        private static void RegisterContext(IServiceCollection serviceCollection, Type type, object context)
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
