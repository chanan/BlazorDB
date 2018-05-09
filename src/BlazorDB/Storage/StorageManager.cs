using System;
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
                    var storageSet = GetStorageSet(storageSetType, contextType, modelType);
                    prop.SetValue(context, storageSet);
                }
            }
            RegisterContext(serviceCollection, contextType, context);
            Logger.EndGroup();
        }

        private static int GetListCount(StorageContext context, PropertyInfo prop)
        {
            var list = prop.GetValue(context);
            var countProp = list.GetType().GetProperty("Count");
            return (int)countProp.GetValue(list);
        }

        private static object GetStorageSet(Type storageSetType, Type contextType, Type modelType)
        {
            var storageTableName = Util.GetStorageTableName(contextType, modelType);
            var value = BlazorDBInterop.GetItem(storageTableName, false);
            var instance = Activator.CreateInstance(storageSetType);
            var prop = storageSetType.GetProperty("StorageContextTypeName", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            prop.SetValue(instance, Util.GetFullyQualifiedTypeName(contextType));
            return value != null ? SetList(instance, Deserialize(modelType, value)) : instance;
        }

        private static object SetList(object instance, object list)
        {
            var prop = instance.GetType().GetProperty("List", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            prop.SetValue(instance, list);
            return instance;
        }

        private static object Deserialize(Type modelType, string value)
        {
            var method = typeof(JsonWrapper).GetMethod("Deserialize");
            var listGenericType = genericListType.MakeGenericType(modelType);
            var genericMethod = method.MakeGenericMethod(listGenericType);
            var list = genericMethod.Invoke(new JsonWrapper(), new object[] { value });
            return list;
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
