using System;

namespace BlazorDB
{
    internal static class Util
    {
        internal static string GetStorageTableName(Type Context, Type Model)
        {
            var databaseName = GetFullyQualifiedTypeName(Context);
            var tableName = GetFullyQualifiedTypeName(Model);
            return $"{databaseName}-{tableName}";
        }

        internal static object GetFullyQualifiedTypeName(Type type)
        {
            return $"{type.Namespace}.{type.Name}";
        }
    }
}
