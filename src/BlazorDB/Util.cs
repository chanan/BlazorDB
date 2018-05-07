using System;

namespace BlazorDB
{
    internal static class Util
    {
        public static string GetStorageTableName(Type Context, Type Model)
        {
            var databaseName = GetFullyQualifiedTypeName(Context);
            var tableName = GetFullyQualifiedTypeName(Model);
            return $"{databaseName}-{tableName}";
        }

        private static object GetFullyQualifiedTypeName(Type type)
        {
            return $"{type.Namespace}.{type.Name}";
        }
    }
}
