using System;

namespace BlazorDB.Storage
{
    internal static class Logger
    {
        internal static bool LogDebug { get; set; } = true;
        internal static readonly string blue = "color: blue; font-style: bold;";
        internal static readonly string green = "color: green; font-style: bold;";
        internal static readonly string red = "color: red; font-style: bold;";
        internal static readonly string normal = "color: black; font-style: normal;";

        internal static void StartContextType(Type contextType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupCollapsed($"Context loaded: %c{contextType.Namespace}.{contextType.Name}", blue);
        }

        internal static void ContextSaved(Type contextType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupCollapsed($"Context %csaved: %c{contextType.Namespace}.{contextType.Name}", green, blue);
        }

        internal static void StorageSetSaved(Type modelType, int count)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log($"StorageSet %csaved:  %c{modelType.Namespace}.{modelType.Name}%c with {count} items", green, blue, normal);
        }

        internal static void EndGroup()
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupEnd();
        }

        internal static void ItemAddedToContext(string contextTypeName, Type modelType, int id, object item)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupCollapsed($"Item  %c{modelType.Namespace}.{modelType.Name}%c %cadded%c to context: %c{contextTypeName}%c with id: {id}", blue, normal, green, normal, blue, normal);
            BlazorLogger.Logger.Log("Item: %o", item);
            BlazorLogger.Logger.GroupEnd();
        }

        internal static void ItemAddedToContext(string contextTypeName, Type modelType, object item)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupCollapsed($"Item  %c{modelType.Namespace}.{modelType.Name}%c %cadded%c to context: %c{contextTypeName}", blue, normal, green, normal, blue);
            BlazorLogger.Logger.Log("Item: %o", item);
            BlazorLogger.Logger.GroupEnd();
        }

        internal static void LoadModelInContext(Type modelType, int count)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log($"StorageSet loaded:  %c{modelType.Namespace}.{modelType.Name}%c with {count} items", blue, normal);
        }

        internal static void ItemRemovedFromContext(string contextTypeName, Type modelType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log($"Item  %c{modelType.Namespace}.{modelType.Name}%c %cremoved%c from context: %c{contextTypeName}", blue, normal, red, normal, blue);
        }
    }
}
