using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal class StorageManagerUtil : IStorageManagerUtil
    {
        public const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public const string Metadata = "metadata";
        public const string GetEnumerator = "GetEnumerator";
        public const string Id = "Id";
        public const string StorageContextTypeName = "StorageContextTypeName";
        public const string Add = "Add";
        public const string Deserialize = "Deserialize";
        public const string List = "List";
        public static readonly Type GenericStorageSetType = typeof(StorageSet<>);
        public static readonly Type GenericListType = typeof(List<>);
        public const string JsonId = "id";

        private readonly IBlazorDBInterop _blazorDBInterop;

        public StorageManagerUtil(IBlazorDBInterop blazorDBInterop)
        {
            _blazorDBInterop = blazorDBInterop;
        }

        public List<PropertyInfo> GetStorageSets(Type contextType)
        {
            return (from prop in contextType.GetProperties()
                    where prop.PropertyType.IsGenericType &&
                          prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>)
                    select prop).ToList();
        }

        public async Task<Metadata> LoadMetadata(string storageTableName)
        {
            string name = $"{storageTableName}-{Metadata}";
            string value = await _blazorDBInterop.GetItem(name, false);
            return value != null ? JsonSerializer.Deserialize<Metadata>(value) : null;
        }

        public string ReplaceString(string source, int start, int end, string stringToInsert)
        {
            string startStr = source.Substring(0, start);
            string endStr = source.Substring(end);
            return startStr + stringToInsert + endStr;
        }

        public bool IsInContext(List<PropertyInfo> storageSets, PropertyInfo prop)
        {
            IEnumerable<PropertyInfo> query = from p in storageSets
                                              where p.PropertyType.GetGenericArguments()[0] == prop.PropertyType
                                              select p;
            return query.SingleOrDefault() != null;
        }

        public bool IsListInContext(List<PropertyInfo> storageSets, PropertyInfo prop)
        {
            IEnumerable<PropertyInfo> query = from p in storageSets
                                              where prop.PropertyType.IsGenericType &&
                                                    p.PropertyType.GetGenericArguments()[0] == prop.PropertyType.GetGenericArguments()[0]
                                              select p;
            return query.SingleOrDefault() != null;
        }
    }
}