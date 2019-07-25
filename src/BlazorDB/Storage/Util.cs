using System;

namespace BlazorDB
{
    internal static class Util
    {
        internal static string GetStorageTableName(Type Context, Type Model)
        {
            string databaseName = GetFullyQualifiedTypeName(Context);
            string tableName = GetFullyQualifiedTypeName(Model);
            return $"{databaseName}-{tableName}";
        }

        internal static string GetFullyQualifiedTypeName(Type type)
        {
            return $"{type.Namespace}.{type.Name}";
        }
    }
}
