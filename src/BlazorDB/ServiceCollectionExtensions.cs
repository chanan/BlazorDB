using BlazorDB.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorDB
{
    public static class ServiceCollectionExtensions
    {
        private static readonly IStorageManager StorageManager = new StorageManager();
        private static readonly Type storageContext = typeof(StorageContext);

        public static IServiceCollection AddBlazorDB(this IServiceCollection serviceCollection, Action<Options> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var options = new Options();
            configure(options);
            if (options.LogDebug) Logger.LogDebug = true;
            Scan(serviceCollection, options.Assembly);
            return serviceCollection;
        }

        private static void Scan(IServiceCollection serviceCollection, Assembly assembly)
        {
            IEnumerable<Type> types = ScanForContexts(serviceCollection, assembly);
            RegisterBlazorDB(serviceCollection, types);
        }

        private static void RegisterBlazorDB(IServiceCollection serviceCollection, IEnumerable<Type> types)
        {
            serviceCollection.AddSingleton<IStorageManager>(StorageManager);
            foreach(var contextType in types)
            {
                StorageManager.LoadContextFromStorageOrCreateNew(serviceCollection, contextType);
            }
        }

        private static IEnumerable<Type> ScanForContexts(IServiceCollection serviceCollection, Assembly assembly)
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(x => storageContext.IsAssignableFrom(x))
                .ToList();
            return types;
        }        
    }
}
