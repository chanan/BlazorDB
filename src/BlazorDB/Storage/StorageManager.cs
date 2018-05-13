using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Blazor;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorDB.Storage
{
    internal class StorageManager : IStorageManager
    {
        private static readonly Type storageContext = typeof(StorageContext);
        private static readonly Type genericStorageSetType = typeof(StorageSet<>);
        private static readonly Type genericListType = typeof(List<>);
        private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public int SaveContextToLocalStorage(StorageContext context)
        {
            int total = 0;
            var contextType = context.GetType();
            Logger.ContextSaved(contextType);
            var storageSets = GetStorageSets(contextType);
            foreach (var prop in storageSets)
            {
                var storageSetValue = prop.GetValue(context);
                var modelType = prop.PropertyType.GetGenericArguments()[0];
                var storageTableName = Util.GetStorageTableName(contextType, modelType);
                var guids = SaveModels(storageSetValue, modelType, storageTableName, storageSets);
                total += guids.Count;
                var oldMetadata = LoadMetadata(storageTableName);
                SaveMetadata(storageTableName, guids, contextType, modelType);
                if (oldMetadata != null) DeleteOldModelsFromStorage(oldMetadata, storageTableName);
                Logger.StorageSetSaved(modelType, guids.Count);
            }
            Logger.EndGroup();
            return total;
        }

        public void LoadContextFromStorageOrCreateNew(IServiceCollection serviceCollection, Type contextType)
        {
            var context = Activator.CreateInstance(contextType);
            Logger.StartContextType(contextType);
            var storageSets = GetStorageSets(contextType);
            var stringModels = LoadStringModels(contextType, storageSets);
            //PrintStringModels(stringModels);
            stringModels = ScanNonAssociationModels(storageSets, stringModels);
            stringModels = ScanAssociationModels(storageSets, stringModels);
            stringModels = DeserializeModels(stringModels);
            foreach (var prop in contextType.GetProperties())
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                {
                    var modelType = prop.PropertyType.GetGenericArguments()[0];
                    var storageSetType = genericStorageSetType.MakeGenericType(modelType);
                    var storageTableName = Util.GetStorageTableName(contextType, modelType);
                    var metadata = LoadMetadata(storageTableName);
                    if(stringModels.ContainsKey(modelType))
                    {
                        var map = stringModels[modelType];
                        Logger.LoadModelInContext(modelType, map.Count);
                    }
                    else
                    {
                        Logger.LoadModelInContext(modelType, 0);
                    }
                    var storageSet = metadata != null ? LoadStorageSet(metadata, storageTableName, storageSetType, contextType, modelType, stringModels[modelType]) : CreateNewStorageSet(storageSetType);
                    prop.SetValue(context, storageSet);
                }
            }
            RegisterContext(serviceCollection, contextType, context);
            Logger.EndGroup();
        }

        private Dictionary<Type, Dictionary<int, SerializedModel>> DeserializeModels(Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            foreach (var map in stringModels)
            {
                Type modelType = map.Key;
                foreach (var sm in map.Value)
                {
                    var stringModel = sm.Value;
                    stringModel.Model = DeserializeModel(modelType, stringModel.StringModel);
                }
            }
            return stringModels;
        }

        private object LoadStorageSet(Metadata metadata, string storageTableName, Type storageSetType, Type contextType, Type modelType, Dictionary<int, SerializedModel> map)
        {
            var instance = CreateNewStorageSet(storageSetType);
            var prop = storageSetType.GetProperty("StorageContextTypeName", flags);
            prop.SetValue(instance, Util.GetFullyQualifiedTypeName(contextType));
            var listGenericType = genericListType.MakeGenericType(modelType);
            var list = Activator.CreateInstance(listGenericType);
            foreach (var sm in map)
            {
                var stringModel = sm.Value;
                var addMethod = listGenericType.GetMethod("Add");
                addMethod.Invoke(list, new object[] { stringModel.Model });
            }
            return SetList(instance, list);
        }

        private Dictionary<Type, Dictionary<int, SerializedModel>> ScanNonAssociationModels(List<PropertyInfo> storageSets, Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            foreach (var map in stringModels)
            {
                Type modelType = map.Key;
                foreach (var sm in map.Value)
                {
                    var stringModel = sm.Value;
                    if (!HasAssociation(storageSets, modelType, stringModel))
                    {
                        stringModel.HasAssociation = false;
                        stringModel.ScanDone = true;
                    }
                    else
                    {
                        stringModel.HasAssociation = true;
                    }
                }
            }
            return stringModels;
        }

        private bool HasAssociation(List<PropertyInfo> storageSets, Type modelType, SerializedModel stringModel)
        {
            var found = false;
            foreach (var prop in modelType.GetProperties())
            {
                if (IsInContext(storageSets, prop))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        private Dictionary<Type, Dictionary<int, SerializedModel>> ScanAssociationModels(List<PropertyInfo> storageSets, Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            var count = 0;
            do
            {
                count++;
                foreach (var map in stringModels)
                {
                    Type modelType = map.Key;
                    foreach (var sm in map.Value)
                    {
                        var stringModel = sm.Value;
                        if (!stringModel.ScanDone)
                        {
                            if(HasAssociation(storageSets, modelType, stringModel))
                            {
                                stringModel.StringModel = FixAssociationsInStringModels(stringModel, modelType, storageSets, stringModels);
                            }
                            else
                            {
                                stringModel.ScanDone = true;
                            }
                        }
                    }
                }
                if (count == 5) break;
            } while (IsScanDone(stringModels));
            return stringModels;
        }

        void PrintStringModels(Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            foreach (var map in stringModels)
            {
                Type modelType = map.Key;
                Console.WriteLine("modelType: {0}", modelType.Name);
                foreach (var sm in map.Value)
                {
                    var stringModel = sm.Value;
                    Console.WriteLine("sm: {0}", sm.Value.StringModel);
                    Console.WriteLine("Is Done: {0}", sm.Value.ScanDone);
                }
            }

        }

        private string FixAssociationsInStringModels(SerializedModel stringModel, Type modelType, List<PropertyInfo> storageSets, Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            var result = stringModel.StringModel;
            foreach (var prop in modelType.GetProperties())
            {
                if (IsInContext(storageSets, prop))
                {
                    var Id = FindIdInSerializedModel(result);
                    var updated = GetAssociatedStringModel(stringModels, prop.PropertyType, Id);
                    result = ReplaceIdWithAssociation(result, prop.Name, Id, updated);
                }
            }
            return result;
        }

        private string GetAssociatedStringModel(Dictionary<Type, Dictionary<int, SerializedModel>> stringModels, Type modelType, int id)
        {
            var map = stringModels[modelType];
            return map[id].StringModel;
        }

        //TODO: Convert to Linq
        private bool IsScanDone(Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            var done = true;
            foreach(var map in stringModels.Values)
            {
                foreach(var sm in map.Values)
                {
                    if (!sm.ScanDone) done = false;
                }
            }
            return done;
        }

        private Dictionary<Type, Dictionary<int, SerializedModel>> LoadStringModels(Type contextType, List<PropertyInfo> storageSets)
        {
            var stringModels = new Dictionary<Type, Dictionary<int, SerializedModel>>();
            foreach (var prop in storageSets)
            {
                var modelType = prop.PropertyType.GetGenericArguments()[0];
                var map = new Dictionary<int, SerializedModel>();
                var storageTableName = Util.GetStorageTableName(contextType, modelType);
                var metadata = LoadMetadata(storageTableName);
                if(metadata != null)
                {
                    foreach (var guid in metadata.Guids)
                    {
                        var name = $"{storageTableName}-{guid}";
                        var serializedModel = BlazorDBInterop.GetItem(name, false);
                        var Id = FindIdInSerializedModel(serializedModel);
                        map.Add(Id, new SerializedModel { StringModel = serializedModel });
                    }
                    stringModels.Add(modelType, map);
                }
            }
            return stringModels;
        }

        //TODO: Verify that the found id is at the top level in case of nested objects
        private int FindIdInSerializedModel(string serializedModel)
        {
            var start = serializedModel.IndexOf("\"Id\":");
            return GetIdFromString(serializedModel, start);
        }

        private int GetIdFromString(string stringToSearch, int startFrom = 0)
        {
            var foundFirst = false;
            var arr = stringToSearch.ToCharArray();
            var result = new List<char>();
            for (int i = startFrom; i < arr.Length; i++)
            {
                var ch = arr[i];
                if (Char.IsDigit(ch))
                {
                    foundFirst = true;
                    result.Add(ch);
                }
                else
                {
                    if (foundFirst) break;
                }
            }
            return Convert.ToInt32(new string(result.ToArray()));
        }

        private List<PropertyInfo> GetStorageSets(Type contextType)
        {
            return (from prop in contextType.GetProperties()
                    where prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>)
                    select prop).ToList();
        }

        private void DeleteOldModelsFromStorage(Metadata metadata, string storageTableName)
        {
            foreach (var guid in metadata.Guids)
            {
                var name = $"{storageTableName}-{guid}";
                BlazorDBInterop.RemoveItem(name, false);
            }
        }

        private void SaveMetadata(string storageTableName, List<Guid> guids, Type context, Type modelType)
        {
            var metadata = new Metadata { Guids = guids, ContextName = Util.GetFullyQualifiedTypeName(context), ModelName = Util.GetFullyQualifiedTypeName(modelType) };
            var name = $"{storageTableName}-metadata";
            BlazorDBInterop.SetItem(name, JsonUtil.Serialize(metadata), false);

        }

        private List<Guid> SaveModels(object storageSetValue, Type modelType, string storageTableName, List<PropertyInfo> storageSets)
        {
            var guids = new List<Guid>();
            var storageSetType = genericStorageSetType.MakeGenericType(modelType);
            var method = storageSetType.GetMethod("GetEnumerator");
            var enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                var guid = Guid.NewGuid();
                guids.Add(guid);
                var model = enumerator.Current;
                var name = $"{storageTableName}-{guid}";
                var serializedModel = ScanModelForAssociations(model, storageSets, JsonUtil.Serialize(model));
                BlazorDBInterop.SetItem(name, serializedModel, false);
            }
            return guids;
        }

        private string ScanModelForAssociations(object model, List<PropertyInfo> storageSets, string serializedModel)
        {
            var result = serializedModel;
            foreach (var prop in model.GetType().GetProperties())
            {
                if(IsInContext(storageSets, prop) && prop.GetValue(model) != null)
                {
                    var associatedModel = prop.GetValue(model);
                    var IdProp = associatedModel.GetType().GetProperty("Id"); //TODO: Handle missing Id prop
                    var id = Convert.ToString(IdProp.GetValue(associatedModel));
                    result = ReplaceAssociationWithId(result, prop.Name, id);
                }
            }
            return result;
        }

        //TODO: A better way to do this would be to use dynamic, which right now doesn't work in Blazor
        private string ReplaceAssociationWithId(string result, string name, string id)
        {
            var stringToFind = $"\"{name}\":{{";
            var start = result.IndexOf(stringToFind);
            var foundFirst = false;
            var arr = result.ToCharArray();
            int count = 0, open = 0, close = 0;
            for(int i = start; i < arr.Length; i++)
            {
                var ch = arr[i];
                if (ch == '{') {
                    count++;
                    foundFirst = true;
                    open = i;
                }
                if (ch == '}') count--;
                if (foundFirst && count == 0)
                {
                    close = i;
                    break;
                }
            }
            result = ReplaceString(result, open, close + 1, id);
            return result;
        }

        private string ReplaceString(string source, int start, int end, string stringToInsert)
        {
            var startStr = source.Substring(0, start);
            var endStr = source.Substring(end);
            return startStr + stringToInsert + endStr;
        }

        private bool IsInContext(List<PropertyInfo> storageSets, PropertyInfo prop)
        {
            var query = from p in storageSets
                        where p.PropertyType.GetGenericArguments()[0] == prop.PropertyType
                        select p;
            return query.SingleOrDefault() !=null;
        }

        private Metadata LoadMetadata(string storageTableName)
        {
            var name = $"{storageTableName}-metadata";
            var value = BlazorDBInterop.GetItem(name, false);
            return value != null ? JsonUtil.Deserialize<Metadata>(value) : null;
        }

        private int GetListCount(StorageContext context, PropertyInfo prop)
        {
            var list = prop.GetValue(context);
            var countProp = list.GetType().GetProperty("Count");
            return (int)countProp.GetValue(list);
        }

        private object CreateNewStorageSet(Type storageSetType)
        {
            return Activator.CreateInstance(storageSetType);
        }

        private string ReplaceIdWithAssociation(string result, string name, int id, string stringModel)
        {
            var stringToFind = $"\"{name}\":{id}";
            var nameIndex = result.IndexOf(stringToFind);
            var index = result.IndexOf(id.ToString(), nameIndex);
            result = ReplaceString(result, index, index + id.ToString().Length, stringModel);
            return result;
        }

        private object SetList(object instance, object list)
        {
            var prop = instance.GetType().GetProperty("List", flags);
            prop.SetValue(instance, list);
            return instance;
        }

        private object DeserializeModel(Type modelType, string value)
        {
            var method = typeof(JsonWrapper).GetMethod("Deserialize");
            var genericMethod = method.MakeGenericMethod(modelType);
            var model = genericMethod.Invoke(new JsonWrapper(), new object[] { value });
            return model;
        }

        private void RegisterContext(IServiceCollection serviceCollection, Type type, object context)
        {
            serviceCollection.AddSingleton(
                serviceType: type,
                implementationInstance: context);
        }
    }

    class JsonWrapper
    {
        public T Deserialize<T>(string value) => JsonUtil.Deserialize<T>(value);
    }
}
