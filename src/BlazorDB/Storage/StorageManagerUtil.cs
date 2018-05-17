using Microsoft.AspNetCore.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorDB.Storage
{
    internal static class StorageManagerUtil
    {
        public static readonly Type storageContext = typeof(StorageContext);
        public static readonly Type genericStorageSetType = typeof(StorageSet<>);
        public static readonly Type genericListType = typeof(List<>);
        public const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public const string METADATA = "metadata";
        public const string GET_ENUMERATOR = "GetEnumerator";
        public const string ID = "Id";
        public const string STORAGE_CONTEXT_TYPE_NAME = "StorageContextTypeName";
        public const string ADD = "Add";
        public const string COUNT = "Count";
        public const string DESERIALIZE = "Deserialize";
        public const string LIST = "List";

        public static List<PropertyInfo> GetStorageSets(Type contextType)
        {
            return (from prop in contextType.GetProperties()
                    where prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>)
                    select prop).ToList();
        }

        public static Metadata LoadMetadata(string storageTableName)
        {
            var name = $"{storageTableName}-{METADATA}";
            var value = BlazorDBInterop.GetItem(name, false);
            return value != null ? JsonUtil.Deserialize<Metadata>(value) : null;
        }
        public static bool IsInContext(List<PropertyInfo> storageSets, PropertyInfo prop)
        {
            var query = from p in storageSets
                        where p.PropertyType.GetGenericArguments()[0] == prop.PropertyType
                        select p;
            return query.SingleOrDefault() != null;
        }

        public static string ReplaceString(string source, int start, int end, string stringToInsert)
        {
            var startStr = source.Substring(0, start);
            var endStr = source.Substring(end);
            return startStr + stringToInsert + endStr;
        }

        internal static bool IsListInContext(List<PropertyInfo> storageSets, PropertyInfo prop)
        {
            var query = from p in storageSets
                        where prop.PropertyType.IsGenericType && 
                            p.PropertyType.GetGenericArguments()[0] == prop.PropertyType.GetGenericArguments()[0]
                        select p;
            return query.SingleOrDefault() != null;
        }
    }
}
