using Microsoft.AspNetCore.Blazor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorDB
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Type storageContext = typeof(StorageContext);
        private static readonly Type genericStorageSetType = typeof(StorageSet<>);
        private static readonly Type genericListType = typeof(List<>);
        public static IServiceCollection AddBlazorDB(this IServiceCollection serviceCollection, Assembly assembly)
        {
            Scan(serviceCollection, assembly);
            return serviceCollection;
        }

        private static void Scan(IServiceCollection serviceCollection, Assembly assembly)
        {
            IEnumerable<Type> types = ScanForContexts(serviceCollection, assembly);
            RegisterBlazorDB(serviceCollection, types);
        }

        private static void RegisterBlazorDB(IServiceCollection serviceCollection, IEnumerable<Type> types)
        {
            foreach(var type in types)
            {
                var context = Activator.CreateInstance(type);
                foreach(var prop in type.GetProperties())
                {
                    if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                    {
                        var modelType = prop.PropertyType.GetGenericArguments()[0];
                        var storageSetType = genericStorageSetType.MakeGenericType(modelType);
                        var storageSet = GetStorageSet(storageSetType, type, modelType);
                        prop.SetValue(context, storageSet);
                    }
                }
                RegisterContext(serviceCollection, type, context);
            }
        }

        private static object GetStorageSet(Type storageSetType, Type type, Type modelType)
        {
            var storageTableName = Util.GetStorageTableName(type, modelType);
            var value = BlazorDBInterop.GetItem(storageTableName, false);
            var instance = Activator.CreateInstance(storageSetType);
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
            var x = genericMethod.Invoke(new JsonWrapper(), new object[] { value });
            return x;
        }

        private static IEnumerable<Type> ScanForContexts(IServiceCollection serviceCollection, Assembly assembly)
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(x => storageContext.IsAssignableFrom(x))
                .ToList();
            return types;
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
