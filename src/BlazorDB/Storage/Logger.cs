using System;

namespace BlazorDB.Storage
{
    internal static class Logger
    {
        private const string Blue = "color: blue; font-style: bold;";
        private const string Green = "color: green; font-style: bold;";
        private const string Red = "color: red; font-style: bold;";
        private const string Normal = "color: black; font-style: normal;";
        internal static bool LogDebug { get; set; } = true;

        internal static void LogStorageSetToConsole(Type type, object list)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log($"StorageSet<{type.GetGenericArguments()[0].Name}>: %o", list);
        }

        internal static void StartContextType(Type contextType, bool loading = true)
        {
            if (!LogDebug) return;
            var message = loading ? " loading" : " log";
            BlazorLogger.Logger.GroupCollapsed($"Context{message}: %c{contextType.Namespace}.{contextType.Name}", Blue);
        }

        internal static void ContextSaved(Type contextType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupCollapsed($"Context %csaved: %c{contextType.Namespace}.{contextType.Name}", Green,
                Blue);
        }

        internal static void StorageSetSaved(Type modelType, int count)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log(
                $"StorageSet %csaved:  %c{modelType.Namespace}.{modelType.Name}%c with {count} items", Green, Blue,
                Normal);
        }

        internal static void EndGroup()
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupEnd();
        }

        internal static void ItemAddedToContext(string contextTypeName, Type modelType, object item)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupCollapsed(
                $"Item  %c{modelType.Namespace}.{modelType.Name}%c %cadded%c to context: %c{contextTypeName}", Blue,
                Normal, Green, Normal, Blue);
            BlazorLogger.Logger.Log("Item: %o", item);
            BlazorLogger.Logger.GroupEnd();
        }

        internal static void LoadModelInContext(Type modelType, int count)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log(
                $"StorageSet loaded:  %c{modelType.Namespace}.{modelType.Name}%c with {count} items", Blue, Normal);
        }

        internal static void ItemRemovedFromContext(string contextTypeName, Type modelType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log(
                $"Item  %c{modelType.Namespace}.{modelType.Name}%c %cremoved%c from context: %c{contextTypeName}", Blue,
                Normal, Red, Normal, Blue);
        }
    }
}