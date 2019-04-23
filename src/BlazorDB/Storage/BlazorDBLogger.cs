using BlazorLogger;
using System;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal class BlazorDBLogger : IBlazorDBLogger
    {
        private const string Blue = "color: blue; font-style: bold;";
        private const string Green = "color: green; font-style: bold;";
        private const string Red = "color: red; font-style: bold;";
        private const string Normal = "color: black; font-style: normal;";
        internal static bool LogDebug { get; set; } = true;

        private ILogger _logger;
        public BlazorDBLogger(ILogger logger)
        {
            _logger = logger;
        }

        public async Task LogStorageSetToConsole(Type type, object list)
        {
            if (!LogDebug) return;
            await _logger.Log($"StorageSet<{type.GetGenericArguments()[0].Name}>: %o", list);
        }

        public async Task StartContextType(Type contextType, bool loading = true)
        {
            if (!LogDebug) return;
            var message = loading ? "loading" : "log";
            await _logger.GroupCollapsed($"Context {message}: %c{contextType.Namespace}.{contextType.Name}", Blue);
        }

        public async Task ContextSaved(Type contextType)
        {
            if (!LogDebug) return;
            await _logger.GroupCollapsed($"Context %csaved: %c{contextType.Namespace}.{contextType.Name}", Green,
                Blue);
        }

        public void StorageSetSaved(Type modelType, int count)
        {
            if (!LogDebug) return;
            _logger.Log(
                $"StorageSet %csaved:  %c{modelType.Namespace}.{modelType.Name}%c with {count} items", Green, Blue,
                Normal);
        }

        public void EndGroup()
        {
            if (!LogDebug) return;
            _logger.GroupEnd();
        }

        public void ItemAddedToContext(string contextTypeName, Type modelType, object item)
        {
            if (!LogDebug) return;
            _logger.GroupCollapsed(
                $"Item  %c{modelType.Namespace}.{modelType.Name}%c %cadded%c to context: %c{contextTypeName}", Blue,
                Normal, Green, Normal, Blue);
            _logger.Log("Item: %o", item);
            _logger.GroupEnd();
        }

        public void LoadModelInContext(Type modelType, int count)
        {
            if (!LogDebug) return;
            _logger.Log(
                $"StorageSet loaded:  %c{modelType.Namespace}.{modelType.Name}%c with {count} items", Blue, Normal);
        }

        public void ItemRemovedFromContext(string contextTypeName, Type modelType)
        {
            if (!LogDebug) return;
            _logger.Log(
                $"Item  %c{modelType.Namespace}.{modelType.Name}%c %cremoved%c from context: %c{contextTypeName}", Blue,
                Normal, Red, Normal, Blue);
        }

        public void Error(string error)
        {
            //Always log errors
            _logger.Error(error);
        }
    }
}