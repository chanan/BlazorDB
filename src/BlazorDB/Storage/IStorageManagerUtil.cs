using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    public interface IStorageManagerUtil
    {
        List<PropertyInfo> GetStorageSets(Type contextType);
        bool IsInContext(List<PropertyInfo> storageSets, PropertyInfo prop);
        bool IsListInContext(List<PropertyInfo> storageSets, PropertyInfo prop);
        Task<Metadata> LoadMetadata(string storageTableName);
        string ReplaceString(string source, int start, int end, string stringToInsert);
    }
}