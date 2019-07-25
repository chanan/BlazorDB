using BlazorDB.Storage;
using BlazorLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorDB
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Type StorageContext = typeof(StorageContext);

        public static IServiceCollection AddBlazorDB(this IServiceCollection serviceCollection,
            Action<Options> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            Options options = new Options();
            configure(options);
            if (options.LogDebug)
            {
                BlazorDBLogger.LogDebug = true;
            }

            Scan(serviceCollection, options.Assembly);
            return serviceCollection;
        }

        private static void Scan(IServiceCollection serviceCollection, Assembly assembly)
        {
            IEnumerable<Type> types = ScanForContexts(serviceCollection, assembly);
            serviceCollection.AddJavascriptLogger();
            serviceCollection.AddSingleton<IBlazorDBInterop, BlazorDBInterop>();
            serviceCollection.AddSingleton<IBlazorDBLogger, BlazorDBLogger>();
            serviceCollection.AddSingleton<IStorageManagerUtil, StorageManagerUtil>();
            serviceCollection.AddSingleton<IStorageManager, StorageManager>();
            serviceCollection.AddSingleton<IStorageManagerSave, StorageManagerSave>();
            serviceCollection.AddSingleton<IStorageManagerLoad, StorageManagerLoad>();

            foreach (Type type in types)
            {
                serviceCollection.AddSingleton(type, s =>
                {
                    IJSRuntime jsRuntime = s.GetRequiredService<IJSRuntime>();
                    object instance = Activator.CreateInstance(type);
                    PropertyInfo smProp = type.GetProperty("StorageManager", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    IStorageManager storageManager = s.GetRequiredService<IStorageManager>();
                    smProp.SetValue(instance, storageManager);

                    PropertyInfo lProp = type.GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    IBlazorDBLogger logger = s.GetRequiredService<IBlazorDBLogger>();
                    lProp.SetValue(instance, logger);

                    PropertyInfo smuProp = type.GetProperty("StorageManagerUtil", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    IStorageManagerUtil storageManagerUtil = s.GetRequiredService<IStorageManagerUtil>();
                    smuProp.SetValue(instance, storageManagerUtil);
                    return instance;
                });
            }
        }

        private static IEnumerable<Type> ScanForContexts(IServiceCollection serviceCollection, Assembly assembly)
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(x => StorageContext.IsAssignableFrom(x))
                .ToList();
            return types;
        }
    }
}