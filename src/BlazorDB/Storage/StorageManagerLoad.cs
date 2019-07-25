using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal class StorageManagerLoad : IStorageManagerLoad
    {
        private readonly IBlazorDBLogger _logger;
        private readonly IBlazorDBInterop _blazorDBInterop;
        private readonly IStorageManagerUtil _storageManagerUtil;
        public StorageManagerLoad(IBlazorDBLogger logger, IBlazorDBInterop blazorDBInterop, IStorageManagerUtil storageManagerUtil)
        {
            _logger = logger;
            _blazorDBInterop = blazorDBInterop;
            _storageManagerUtil = storageManagerUtil;
        }

        public async Task LoadContextFromLocalStorage(StorageContext context)
        {
            Type contextType = context.GetType();
            await _logger.StartContextType(contextType);
            List<PropertyInfo> storageSets = _storageManagerUtil.GetStorageSets(contextType);
            Dictionary<Type, Dictionary<int, SerializedModel>> stringModels = await LoadStringModels(contextType, storageSets);
            //PrintStringModels(stringModels);
            stringModels = ScanNonAssociationModels(storageSets, stringModels);
            stringModels = ScanAssociationModels(storageSets, stringModels);
            stringModels = DeserializeModels(stringModels, storageSets);
            //PrintStringModels(stringModels);
            await EnrichContext(context, contextType, stringModels);
            _logger.EndGroup();
        }

        private async Task EnrichContext(StorageContext context, Type contextType,
            IReadOnlyDictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            foreach (PropertyInfo prop in contextType.GetProperties())
            {
                if (prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition() == typeof(StorageSet<>))
                {
                    Type modelType = prop.PropertyType.GetGenericArguments()[0];
                    Type storageSetType = StorageManagerUtil.GenericStorageSetType.MakeGenericType(modelType);
                    string storageTableName = Util.GetStorageTableName(contextType, modelType);
                    Metadata metadata = await _storageManagerUtil.LoadMetadata(storageTableName);
                    if (stringModels.ContainsKey(modelType))
                    {
                        Dictionary<int, SerializedModel> map = stringModels[modelType];
                        _logger.LoadModelInContext(modelType, map.Count);
                    }
                    else
                    {
                        _logger.LoadModelInContext(modelType, 0);
                    }
                    object storageSet = metadata != null
                        ? LoadStorageSet(storageSetType, contextType, modelType, stringModels[modelType])
                        : CreateNewStorageSet(storageSetType, contextType);
                    prop.SetValue(context, storageSet);
                }
            }
        }

        private Dictionary<Type, Dictionary<int, SerializedModel>> DeserializeModels(
            Dictionary<Type, Dictionary<int, SerializedModel>> stringModels, List<PropertyInfo> storageSets)
        {
            foreach (KeyValuePair<Type, Dictionary<int, SerializedModel>> map in stringModels)
            {
                Type modelType = map.Key;
                foreach (KeyValuePair<int, SerializedModel> sm in map.Value)
                {
                    SerializedModel stringModel = sm.Value;
                    if (!stringModel.HasAssociation)
                    {
                        stringModel.Model = DeserializeModel(modelType, stringModel.StringModel);
                    }
                }
            }

            foreach (KeyValuePair<Type, Dictionary<int, SerializedModel>> map in stringModels) //TODO: Fix associations that are more than one level deep
            {
                Type modelType = map.Key;
                foreach (KeyValuePair<int, SerializedModel> sm in map.Value)
                {
                    SerializedModel stringModel = sm.Value;
                    if (stringModel.Model != null)
                    {
                        continue;
                    }

                    object model = DeserializeModel(modelType, stringModel.StringModel);
                    foreach (PropertyInfo prop in model.GetType().GetProperties())
                    {
                        if (_storageManagerUtil.IsInContext(storageSets, prop) && prop.GetValue(model) != null)
                        {
                            object associatedLocalModel = prop.GetValue(model);
                            PropertyInfo localIdProp =
                                associatedLocalModel.GetType()
                                    .GetProperty(StorageManagerUtil.Id);
                            if (localIdProp == null)
                            {
                                throw new ArgumentException("Model must have Id property");
                            }

                            int localId = Convert.ToInt32(localIdProp.GetValue(associatedLocalModel));
                            object associatdRemoteModel =
                                GetModelFromStringModels(stringModels, associatedLocalModel.GetType(), localId)
                                    .Model;
                            prop.SetValue(model, associatdRemoteModel);
                        }
                    }

                    stringModel.Model = model;
                }
            }

            return stringModels;
        }

        private static SerializedModel GetModelFromStringModels(
            IReadOnlyDictionary<Type, Dictionary<int, SerializedModel>> stringModels, Type type, int localId)
        {
            return stringModels[type][localId];
        }

        private object LoadStorageSet(Type storageSetType, Type contextType, Type modelType,
            Dictionary<int, SerializedModel> map)
        {
            object instance = CreateNewStorageSet(storageSetType, contextType);
            Type listGenericType = StorageManagerUtil.GenericListType.MakeGenericType(modelType);
            object list = Activator.CreateInstance(listGenericType);
            foreach (KeyValuePair<int, SerializedModel> sm in map)
            {
                SerializedModel stringModel = sm.Value;
                MethodInfo addMethod = listGenericType.GetMethod(StorageManagerUtil.Add);
                addMethod.Invoke(list, new[] { stringModel.Model });
            }

            return SetList(instance, list);
        }

        private Dictionary<Type, Dictionary<int, SerializedModel>> ScanNonAssociationModels(
            List<PropertyInfo> storageSets, Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            foreach (KeyValuePair<Type, Dictionary<int, SerializedModel>> map in stringModels)
            {
                Type modelType = map.Key;
                foreach (KeyValuePair<int, SerializedModel> sm in map.Value)
                {
                    SerializedModel stringModel = sm.Value;
                    if (!HasAssociation(storageSets, modelType) &&
                        !HasListAssociation(storageSets, modelType))
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

        private bool HasAssociation(List<PropertyInfo> storageSets, Type modelType)
        {
            return modelType.GetProperties().Any(prop => _storageManagerUtil.IsInContext(storageSets, prop));
        }

        private bool HasListAssociation(List<PropertyInfo> storageSets, Type modelType)
        {
            return modelType.GetProperties().Any(prop => _storageManagerUtil.IsListInContext(storageSets, prop));
        }


        //TODO: The snippet below should also check to see that the model itself has no more associations to fix, not just if it has properties.
        /*
         * if(HasAssociation(storageSets, modelType, stringModel))
            {
                stringModel.StringModel = FixAssociationsInStringModels(stringModel, modelType, storageSets, stringModels);
                stringModel.ScanDone = true;
            }*/
        private Dictionary<Type, Dictionary<int, SerializedModel>> ScanAssociationModels(
            List<PropertyInfo> storageSets,
            Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            int count = 0;
            do
            {
                count++;
                foreach (KeyValuePair<Type, Dictionary<int, SerializedModel>> map in stringModels)
                {
                    Type modelType = map.Key;
                    foreach (KeyValuePair<int, SerializedModel> sm in map.Value)
                    {
                        SerializedModel stringModel = sm.Value;
                        if (stringModel.ScanDone)
                        {
                            continue;
                        }

                        if (HasAssociation(storageSets, modelType) ||
                            HasListAssociation(storageSets, modelType))
                        {
                            stringModel.StringModel =
                                FixAssociationsInStringModels(stringModel, modelType, storageSets, stringModels);
                            stringModel.ScanDone = true;
                        }
                        else
                        {
                            stringModel.ScanDone = true;
                        }
                    }
                }

                if (count == 20)
                {
                    break; //Go 20 deep throw exception here?
                }
            } while (IsScanDone(stringModels));

            return stringModels;
        }

        private void PrintStringModels(Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            foreach (KeyValuePair<Type, Dictionary<int, SerializedModel>> map in stringModels)
            {
                Type modelType = map.Key;
                Console.WriteLine("-----------");
                Console.WriteLine("modelType: {0}", modelType.Name);
                foreach (KeyValuePair<int, SerializedModel> sm in map.Value)
                {
                    Console.WriteLine("Key: {0}", sm.Key);
                    Console.WriteLine("sm: {0}", sm.Value.StringModel);
                    Console.WriteLine("Is Done: {0}", sm.Value.ScanDone);
                    Console.WriteLine("Has Model: {0}", sm.Value.Model != null);
                }
            }
        }

        private string FixAssociationsInStringModels(SerializedModel stringModel, Type modelType,
            List<PropertyInfo> storageSets, Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            string result = stringModel.StringModel;
            foreach (PropertyInfo prop in modelType.GetProperties())
            {
                if (_storageManagerUtil.IsInContext(storageSets, prop) &&
                    TryGetIdFromSerializedModel(result, prop.Name, out int id))
                {
                    string updated = GetAssociatedStringModel(stringModels, prop.PropertyType, id);
                    result = ReplaceIdWithAssociation(result, prop.Name, id, updated);
                }

                if (!_storageManagerUtil.IsListInContext(storageSets, prop))
                {
                    continue;
                }

                {
                    if (!TryGetIdListFromSerializedModel(result, prop.Name, out List<int> idList))
                    {
                        continue;
                    }

                    StringBuilder sb = new StringBuilder();
                    foreach (int item in idList)
                    {
                        string updated = GetAssociatedStringModel(stringModels,
                            prop.PropertyType.GetGenericArguments()[0], item);
                        sb.Append(updated).Append(",");
                    }

                    string strList = sb.ToString().Substring(0, sb.ToString().Length - 1);
                    result = ReplaceListWithAssociationList(result, prop.Name, strList);
                }
            }

            return result;
        }

        private string ReplaceListWithAssociationList(string serializedModel, string propName, string strList)
        {
            int propStart = serializedModel.IndexOf($"\"{propName}\":[", StringComparison.Ordinal);
            int start = serializedModel.IndexOf('[', propStart) + 1;
            int end = serializedModel.IndexOf(']', start);
            string result = _storageManagerUtil.ReplaceString(serializedModel, start, end, strList);
            return result;
        }

        private static bool TryGetIdListFromSerializedModel(string serializedModel, string propName,
            out List<int> idList)
        {
            List<int> list = new List<int>();
            if (serializedModel.IndexOf($"\"{propName}\":null", StringComparison.Ordinal) != -1)
            {
                idList = list;
                return false;
            }

            int propStart = serializedModel.IndexOf($"\"{propName}\":[", StringComparison.Ordinal);
            int start = serializedModel.IndexOf('[', propStart) + 1;
            int end = serializedModel.IndexOf(']', start);
            string stringlist = serializedModel.Substring(start, end - start);
            string[] arr = stringlist.Split(',');
            list.AddRange(arr.Select(s => Convert.ToInt32(s)));
            idList = list;
            return true;
        }

        private static string GetAssociatedStringModel(
            IReadOnlyDictionary<Type, Dictionary<int, SerializedModel>> stringModels,
            Type modelType, int id)
        {
            Dictionary<int, SerializedModel> map = stringModels[modelType];
            return map[id].StringModel;
        }

        //TODO: Convert to Linq
        private static bool IsScanDone(Dictionary<Type, Dictionary<int, SerializedModel>> stringModels)
        {
            bool done = true;
            foreach (Dictionary<int, SerializedModel> map in stringModels.Values)
            {
                foreach (SerializedModel sm in map.Values)
                {
                    if (!sm.ScanDone)
                    {
                        done = false;
                    }
                }
            }

            return done;
        }

        private async Task<Dictionary<Type, Dictionary<int, SerializedModel>>> LoadStringModels(Type contextType,
            IEnumerable<PropertyInfo> storageSets)
        {
            Dictionary<Type, Dictionary<int, SerializedModel>> stringModels = new Dictionary<Type, Dictionary<int, SerializedModel>>();
            foreach (PropertyInfo prop in storageSets)
            {
                Type modelType = prop.PropertyType.GetGenericArguments()[0];
                Dictionary<int, SerializedModel> map = new Dictionary<int, SerializedModel>();
                string storageTableName = Util.GetStorageTableName(contextType, modelType);
                Metadata metadata = await _storageManagerUtil.LoadMetadata(storageTableName);
                if (metadata == null)
                {
                    continue;
                }

                foreach (Guid guid in metadata.Guids)
                {
                    string name = $"{storageTableName}-{guid}";
                    string serializedModel = await _blazorDBInterop.GetItem(name, false);
                    int id = FindIdInSerializedModel(serializedModel);
                    map.Add(id, new SerializedModel { StringModel = serializedModel });
                }

                stringModels.Add(modelType, map);
            }

            return stringModels;
        }

        //TODO: Verify that the found id is at the top level in case of nested objects
        private static bool TryGetIdFromSerializedModel(string serializedModel, string propName, out int id)
        {
            if (serializedModel.IndexOf($"\"{propName}\":null", StringComparison.Ordinal) != -1)
            {
                id = -1;
                return false;
            }

            int propStart = serializedModel.IndexOf($"\"{propName}\":", StringComparison.Ordinal);
            int start = serializedModel.IndexOf(':', propStart);
            id = GetIdFromString(serializedModel, start);
            return true;
        }

        //TODO: Verify that the found id is at the top level in case of nested objects
        private static int FindIdInSerializedModel(string serializedModel)
        {
            int start = serializedModel.IndexOf($"\"{StorageManagerUtil.Id}\":", StringComparison.Ordinal);
            return GetIdFromString(serializedModel, start);
        }

        private static int GetIdFromString(string stringToSearch, int startFrom = 0)
        {
            Console.WriteLine("stringToSearch: " + stringToSearch);
            Console.WriteLine("startFrom: " + startFrom);
            bool foundFirst = false;
            char[] arr = stringToSearch.ToCharArray();
            List<char> result = new List<char>();
            for (int i = startFrom; i < arr.Length; i++)
            {
                char ch = arr[i];
                Console.WriteLine("ch: " + ch);
                if (char.IsDigit(ch))
                {
                    foundFirst = true;
                    result.Add(ch);
                }
                else
                {
                    if (foundFirst)
                    {
                        break;
                    }
                }
            }

            return Convert.ToInt32(new string(result.ToArray()));
        }

        private string ReplaceIdWithAssociation(string result, string name, int id, string stringModel)
        {
            string stringToFind = $"\"{name}\":{id}";
            int nameIndex = result.IndexOf(stringToFind, StringComparison.Ordinal);
            int index = result.IndexOf(id.ToString(), nameIndex, StringComparison.Ordinal);
            result = _storageManagerUtil.ReplaceString(result, index, index + id.ToString().Length, stringModel);
            return result;
        }

        private object CreateNewStorageSet(Type storageSetType, Type contextType)
        {
            object instance = Activator.CreateInstance(storageSetType);
            PropertyInfo prop = storageSetType.GetProperty(StorageManagerUtil.StorageContextTypeName,
                StorageManagerUtil.Flags);
            prop.SetValue(instance, Util.GetFullyQualifiedTypeName(contextType));

            PropertyInfo lProp = storageSetType.GetProperty("Logger", StorageManagerUtil.Flags);
            lProp.SetValue(instance, _logger);

            return instance;
        }

        private static object SetList(object instance, object list)
        {
            PropertyInfo prop = instance.GetType().GetProperty(StorageManagerUtil.List, StorageManagerUtil.Flags);
            prop.SetValue(instance, list);
            return instance;
        }

        private static object DeserializeModel(Type modelType, string value)
        {
            MethodInfo method = typeof(JsonWrapper).GetMethod(StorageManagerUtil.Deserialize);
            MethodInfo genericMethod = method.MakeGenericMethod(modelType);
            object model = genericMethod.Invoke(new JsonWrapper(), new object[] { value });
            return model;
        }
    }

    internal class JsonWrapper
    {
        public T Deserialize<T>(string value)
        {
            return JsonSerializer.Deserialize<T>(value);
        }
    }
}