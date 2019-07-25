using BlazorDB.DataAnnotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal class StorageManagerSave : IStorageManagerSave
    {
        private readonly IBlazorDBLogger _logger;
        private readonly IBlazorDBInterop _blazorDBInterop;
        private readonly IStorageManagerUtil _storageManagerUtil;
        public StorageManagerSave(IBlazorDBLogger logger, IBlazorDBInterop blazorDBInterop, IStorageManagerUtil storageManagerUtil)
        {
            _logger = logger;
            _blazorDBInterop = blazorDBInterop;
            _storageManagerUtil = storageManagerUtil;
        }

        public async Task<int> SaveContextToLocalStorage(StorageContext context)
        {
            int total = 0;
            Type contextType = context.GetType();
            await _logger.ContextSaved(contextType);
            List<PropertyInfo> storageSets = _storageManagerUtil.GetStorageSets(contextType);
            string error = ValidateModels(context, storageSets);
            if (error == null)
            {
                IReadOnlyDictionary<string, Metadata> metadataMap = await LoadMetadataList(context, storageSets, contextType);
                total = await SaveStorageSets(context, total, contextType, storageSets, metadataMap);
                _logger.EndGroup();
            }
            else
            {
                _logger.Error("SaveChanges() terminated due to validation error");
                _logger.EndGroup();
                throw new BlazorDBUpdateException(error);
            }

            return total;
        }

        private string ValidateModels(StorageContext context, IEnumerable<PropertyInfo> storageSets)
        {
            string error = null;
            foreach (PropertyInfo storeageSetProp in storageSets)
            {
                object storeageSet = storeageSetProp.GetValue(context);
                PropertyInfo listProp = storeageSet.GetType().GetProperty(StorageManagerUtil.List, StorageManagerUtil.Flags);
                object list = listProp.GetValue(storeageSet);
                MethodInfo method = list.GetType().GetMethod(StorageManagerUtil.GetEnumerator);
                IEnumerator enumerator = (IEnumerator)method.Invoke(list, new object[] { });
                while (enumerator.MoveNext())
                {
                    object model = enumerator.Current;
                    foreach (PropertyInfo prop in model.GetType().GetProperties())
                    {
                        if (Attribute.IsDefined(prop, typeof(Required)))
                        {
                            object value = prop.GetValue(model);
                            if (value == null)
                            {
                                error =
                                    $"{model.GetType().FullName}.{prop.Name} is a required field. SaveChanges() has been terminated.";
                                break;
                            }
                        }

                        if (Attribute.IsDefined(prop, typeof(MaxLength)))
                        {
                            MaxLength maxLength = (MaxLength)Attribute.GetCustomAttribute(prop, typeof(MaxLength));
                            object value = prop.GetValue(model);
                            if (value != null)
                            {
                                string str = value.ToString();
                                if (str.Length > maxLength.length)
                                {
                                    error =
                                        $"{model.GetType().FullName}.{prop.Name} length is longer than {maxLength.length}. SaveChanges() has been terminated.";
                                    break;
                                }
                            }
                        }
                    }

                    if (error != null)
                    {
                        break;
                    }
                }
            }

            return error;
        }

        private async Task<IReadOnlyDictionary<string, Metadata>> LoadMetadataList(StorageContext context,
            IEnumerable<PropertyInfo> storageSets, Type contextType)
        {
            Dictionary<string, Metadata> map = new Dictionary<string, Metadata>();
            foreach (PropertyInfo prop in storageSets)
            {
                Type modelType = prop.PropertyType.GetGenericArguments()[0];
                string storageTableName = Util.GetStorageTableName(contextType, modelType);
                Metadata metadata = await _storageManagerUtil.LoadMetadata(storageTableName) ?? new Metadata
                {
                    Guids = new List<Guid>(),
                    ContextName = Util.GetFullyQualifiedTypeName(context.GetType()),
                    ModelName = Util.GetFullyQualifiedTypeName(modelType)
                };
                map.Add(Util.GetFullyQualifiedTypeName(modelType), metadata);
            }

            return map;
        }

        private async Task<int> SaveStorageSets(StorageContext context, int total, Type contextType,
            List<PropertyInfo> storageSets, IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            foreach (PropertyInfo prop in storageSets)
            {
                object storageSetValue = prop.GetValue(context);
                Type modelType = prop.PropertyType.GetGenericArguments()[0];
                string storageTableName = Util.GetStorageTableName(contextType, modelType);

                EnsureAllModelsHaveIds(storageSetValue, modelType, metadataMap);
                EnsureAllAssociationsHaveIds(context, storageSetValue, modelType, storageSets, metadataMap);

                List<Guid> guids = await SaveModels(storageSetValue, modelType, storageTableName, storageSets);
                total += guids.Count;
                Metadata oldMetadata = metadataMap[Util.GetFullyQualifiedTypeName(modelType)];
                SaveMetadata(storageTableName, guids, contextType, modelType, oldMetadata);
                DeleteOldModelsFromStorage(oldMetadata, storageTableName);
                _logger.StorageSetSaved(modelType, guids.Count);
            }

            return total;
        }

        private void EnsureAllAssociationsHaveIds(StorageContext context, object storageSetValue, Type modelType,
            List<PropertyInfo> storageSets, IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            Type storageSetType = StorageManagerUtil.GenericStorageSetType.MakeGenericType(modelType);
            MethodInfo method = storageSetType.GetMethod(StorageManagerUtil.GetEnumerator);
            IEnumerator enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                object model = enumerator.Current;
                foreach (PropertyInfo prop in model.GetType().GetProperties())
                {
                    if (prop.GetValue(model) == null || !_storageManagerUtil.IsInContext(storageSets, prop) &&
                        !_storageManagerUtil.IsListInContext(storageSets, prop))
                    {
                        continue;
                    }

                    if (_storageManagerUtil.IsInContext(storageSets, prop))
                    {
                        EnsureOneAssociationHasId(context, prop.GetValue(model), prop.PropertyType, storageSets,
                            metadataMap);
                    }

                    if (_storageManagerUtil.IsListInContext(storageSets, prop))
                    {
                        EnsureManyAssociationHasId(context, prop.GetValue(model), prop, storageSets, metadataMap);
                    }
                }
            }
        }

        private static void EnsureManyAssociationHasId(StorageContext context, object listObject, PropertyInfo prop,
            List<PropertyInfo> storageSets, IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            MethodInfo method = listObject.GetType().GetMethod(StorageManagerUtil.GetEnumerator);
            IEnumerator enumerator = (IEnumerator)method.Invoke(listObject, new object[] { });
            while (enumerator.MoveNext())
            {
                object model = enumerator.Current;
                EnsureOneAssociationHasId(context, model, prop.PropertyType.GetGenericArguments()[0], storageSets,
                    metadataMap);
            }
        }

        private static void EnsureOneAssociationHasId(StorageContext context, object associatedModel, Type propType,
            List<PropertyInfo> storageSets, IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            PropertyInfo idProp = GetIdProperty(associatedModel);
            string id = Convert.ToString(idProp.GetValue(associatedModel));
            Metadata metadata = metadataMap[Util.GetFullyQualifiedTypeName(propType)];
            if (id == "0")
            {
                metadata.MaxId = metadata.MaxId + 1;
                SaveAssociationModel(context, associatedModel, propType, storageSets, metadata.MaxId);
            }
            else
            {
                EnsureAssociationModelExistsOrThrow(context, Convert.ToInt32(id), storageSets, propType);
            }
        }

        private static void EnsureAssociationModelExistsOrThrow(StorageContext context, int id,
            IEnumerable<PropertyInfo> storageSets, Type propType)
        {
            IEnumerable<PropertyInfo> q = from p in storageSets
                                          where p.PropertyType.GetGenericArguments()[0] == propType
                                          select p;
            PropertyInfo storeageSetProp = q.Single();
            object storeageSet = storeageSetProp.GetValue(context);
            PropertyInfo listProp = storeageSet.GetType().GetProperty(StorageManagerUtil.List, StorageManagerUtil.Flags);
            object list = listProp.GetValue(storeageSet);
            MethodInfo method = list.GetType().GetMethod(StorageManagerUtil.GetEnumerator);
            IEnumerator enumerator = (IEnumerator)method.Invoke(list, new object[] { });
            bool found = false;
            while (enumerator.MoveNext())
            {
                object model = enumerator.Current;
                if (id != GetId(model))
                {
                    continue;
                }

                found = true;
                break;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"A model of type: {propType.Name} with Id {id} was deleted but still being used by an association. Remove it from the association as well.");
            }
        }

        private static void EnsureAllModelsHaveIds(object storageSetValue, Type modelType,
            IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            Type storageSetType = StorageManagerUtil.GenericStorageSetType.MakeGenericType(modelType);
            MethodInfo method = storageSetType.GetMethod(StorageManagerUtil.GetEnumerator);
            Metadata metadata = metadataMap[Util.GetFullyQualifiedTypeName(modelType)];
            IEnumerator enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                object model = enumerator.Current;
                if (GetId(model) != 0)
                {
                    continue;
                }

                metadata.MaxId = metadata.MaxId + 1;
                SetId(model, metadata.MaxId);
            }
        }

        private async void SaveMetadata(string storageTableName, List<Guid> guids, Type context, Type modelType,
            Metadata oldMetadata)
        {
            Metadata metadata = new Metadata
            {
                Guids = guids,
                ContextName = Util.GetFullyQualifiedTypeName(context),
                ModelName = Util.GetFullyQualifiedTypeName(modelType),
                MaxId = oldMetadata.MaxId
            };
            string name = $"{storageTableName}-{StorageManagerUtil.Metadata}";
            await _blazorDBInterop.SetItem(name, JsonSerializer.Serialize(metadata), false);
        }

        private async Task<List<Guid>> SaveModels(object storageSetValue, Type modelType, string storageTableName,
            List<PropertyInfo> storageSets)
        {
            List<Guid> guids = new List<Guid>();
            Type storageSetType = StorageManagerUtil.GenericStorageSetType.MakeGenericType(modelType);
            MethodInfo method = storageSetType.GetMethod(StorageManagerUtil.GetEnumerator);
            IEnumerator enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                Guid guid = Guid.NewGuid();
                guids.Add(guid);
                object model = enumerator.Current;
                string name = $"{storageTableName}-{guid}";
                string serializedModel = ScanModelForAssociations(model, storageSets, JsonSerializer.Serialize(model));
                await _blazorDBInterop.SetItem(name, serializedModel, false);
            }

            return guids;
        }

        private void DeleteOldModelsFromStorage(Metadata metadata, string storageTableName)
        {
            foreach (Guid guid in metadata.Guids)
            {
                string name = $"{storageTableName}-{guid}";
                _blazorDBInterop.RemoveItem(name, false);
            }
        }

        private string ScanModelForAssociations(object model, List<PropertyInfo> storageSets,
            string serializedModel)
        {
            string result = serializedModel;
            foreach (PropertyInfo prop in model.GetType().GetProperties())
            {
                if (prop.GetValue(model) == null || !_storageManagerUtil.IsInContext(storageSets, prop) &&
                    !_storageManagerUtil.IsListInContext(storageSets, prop))
                {
                    continue;
                }

                if (_storageManagerUtil.IsInContext(storageSets, prop))
                {
                    result = FixOneAssociation(model, prop, result);
                }

                if (_storageManagerUtil.IsListInContext(storageSets, prop))
                {
                    result = FixManyAssociation(model, prop, result);
                }
            }

            return result;
        }

        private static string FixManyAssociation(object model, PropertyInfo prop, string result)
        {
            IEnumerable modelList = (IEnumerable)prop.GetValue(model);
            foreach (object item in modelList)
            {
                PropertyInfo idProp = GetIdProperty(item);
                string id = Convert.ToString(idProp.GetValue(item));
                string serializedItem = JsonSerializer.Serialize(item);
                result = ReplaceModelWithId(result, serializedItem, id);
            }

            return result;
        }

        private static string FixOneAssociation(object model, PropertyInfo prop, string result)
        {
            object associatedModel = prop.GetValue(model);
            PropertyInfo idProp = GetIdProperty(associatedModel);
            string id = Convert.ToString(idProp.GetValue(associatedModel));
            string serializedItem = JsonSerializer.Serialize(associatedModel);
            result = ReplaceModelWithId(result, serializedItem, id);
            return result;
        }

        private static int SaveAssociationModel(StorageContext context, object associatedModel, Type propType,
            IEnumerable<PropertyInfo> storageSets, int id)
        {
            IEnumerable<PropertyInfo> q = from p in storageSets
                                          where p.PropertyType.GetGenericArguments()[0] == propType
                                          select p;
            PropertyInfo storeageSetProp = q.Single();
            object storeageSet = storeageSetProp.GetValue(context);
            PropertyInfo listProp = storeageSet.GetType().GetProperty(StorageManagerUtil.List, StorageManagerUtil.Flags);
            object list = listProp.GetValue(storeageSet);
            MethodInfo addMethod = list.GetType().GetMethod(StorageManagerUtil.Add);
            SetId(associatedModel, id);
            addMethod.Invoke(list, new[] { associatedModel });
            return id;
        }

        private static string ReplaceModelWithId(string result, string serializedItem, string id)
        {
            return result.Replace(serializedItem, id);
        }

        private static int GetId(object item)
        {
            PropertyInfo prop = GetIdProperty(item);
            return (int)prop.GetValue(item);
        }

        private static void SetId(object item, int id)
        {
            PropertyInfo prop = GetIdProperty(item);
            prop.SetValue(item, id);
        }

        private static PropertyInfo GetIdProperty(object item)
        {
            PropertyInfo prop = item.GetType().GetProperty(StorageManagerUtil.Id);
            if (prop == null)
            {
                throw new ArgumentException("Model must have an Id property");
            }

            return prop;
        }
    }
}