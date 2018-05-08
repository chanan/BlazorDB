using System;

namespace BlazorDB
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
            BlazorLogger.Logger.GroupCollapsed($"Context Loaded: %c{contextType.Namespace}.{contextType.Name}", blue);
        }

        internal static void EndContextType()
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.GroupEnd();
        }

        internal static void ItemAddedToContext(string contextTypeName, Type modelType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log($"Item  %c{modelType.Namespace}.{modelType.Name}%c %cadded%c to Context: %c{contextTypeName}", blue, normal, green, normal, blue);
        }

        internal static void LoadModelInContext(Type modelType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log($"StorageContext Loaded:  %c{modelType.Namespace}.{modelType.Name}", blue);
        }

        internal static void ItemRemovedFromContext(string contextTypeName, Type modelType)
        {
            if (!LogDebug) return;
            BlazorLogger.Logger.Log($"Item  %c{modelType.Namespace}.{modelType.Name}%c %cremoved%c from Context: %c{contextTypeName}", blue, normal, red, normal, blue);
        }
    }
}
