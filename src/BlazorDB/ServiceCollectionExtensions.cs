using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlazorDB.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorDB
{
    public static class ServiceCollectionExtensions
    {
        private static readonly IStorageManager StorageManager = new StorageManager();
        private static readonly Type StorageContext = typeof(StorageContext);

        public static IServiceCollection AddBlazorDB(this IServiceCollection serviceCollection,
            Action<Options> configure)
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
            var types = ScanForContexts(serviceCollection, assembly);
            RegisterBlazorDb(serviceCollection, types);
        }

        private static void RegisterBlazorDb(IServiceCollection serviceCollection, IEnumerable<Type> types)
        {
            serviceCollection.AddSingleton(StorageManager);
            foreach (var contextType in types)
                StorageManager.LoadContextFromStorageOrCreateNew(serviceCollection, contextType);
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