using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorDB.Storage
{
    internal static class StorageManagerUtil
    {
        internal const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        internal const string Metadata = "metadata";
        internal const string GetEnumerator = "GetEnumerator";
        internal const string Id = "Id";
        internal const string StorageContextTypeName = "StorageContextTypeName";
        internal const string Add = "Add";
        internal const string Deserialize = "Deserialize";
        internal const string List = "List";
        internal static readonly Type GenericStorageSetType = typeof(StorageSet<>);
        internal static readonly Type GenericListType = typeof(List<>);
        internal const string JsonId = "id";

        internal static List<PropertyInfo> GetStorageSets(Type contextType)
        {
            return (from prop in contextType.GetProperties()
                where prop.PropertyType.IsGenericType &&
                      prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>)
                select prop).ToList();
        }

        internal static async Task<Metadata> LoadMetadata(string storageTableName)
        {
            var name = $"{storageTableName}-{Metadata}";
            var value = await BlazorDBInterop.GetItem(name, false);
            return value != null ? Json.Deserialize<Metadata>(value) : null;
        }

        internal static string ReplaceString(string source, int start, int end, string stringToInsert)
        {
            var startStr = source.Substring(0, start);
            var endStr = source.Substring(end);
            return startStr + stringToInsert + endStr;
        }

        internal static bool IsInContext(List<PropertyInfo> storageSets, PropertyInfo prop)
        {
            var query = from p in storageSets
                where p.PropertyType.GetGenericArguments()[0] == prop.PropertyType
                select p;
            return query.SingleOrDefault() != null;
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