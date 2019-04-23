using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlazorDB.DataAnnotations;
using Microsoft.JSInterop;

namespace BlazorDB.Storage
{
    internal class StorageManagerSave : IStorageManagerSave
    {
        private IBlazorDBLogger _logger;
        private IBlazorDBInterop _blazorDBInterop;
        private IStorageManagerUtil _storageManagerUtil;
        public StorageManagerSave(IBlazorDBLogger logger, IBlazorDBInterop blazorDBInterop, IStorageManagerUtil storageManagerUtil)
        {
            _logger = logger;
            _blazorDBInterop = blazorDBInterop;
            _storageManagerUtil = storageManagerUtil;
        }

        public async Task<int> SaveContextToLocalStorage(StorageContext context)
        {
            var total = 0;
            var contextType = context.GetType();
            await _logger.ContextSaved(contextType);
            var storageSets = _storageManagerUtil.GetStorageSets(contextType);
            var error = ValidateModels(context, storageSets);
            if (error == null)
            {
                var metadataMap = await LoadMetadataList(context, storageSets, contextType);
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
            foreach (var storeageSetProp in storageSets)
            {
                var storeageSet = storeageSetProp.GetValue(context);
                var listProp = storeageSet.GetType().GetProperty(StorageManagerUtil.List, StorageManagerUtil.Flags);
                var list = listProp.GetValue(storeageSet);
                var method = list.GetType().GetMethod(StorageManagerUtil.GetEnumerator);
                var enumerator = (IEnumerator)method.Invoke(list, new object[] { });
                while (enumerator.MoveNext())
                {
                    var model = enumerator.Current;
                    foreach (var prop in model.GetType().GetProperties())
                    {
                        if (Attribute.IsDefined(prop, typeof(Required)))
                        {
                            var value = prop.GetValue(model);
                            if (value == null)
                            {
                                error =
                                    $"{model.GetType().FullName}.{prop.Name} is a required field. SaveChanges() has been terminated.";
                                break;
                            }
                        }

                        if (Attribute.IsDefined(prop, typeof(MaxLength)))
                        {
                            var maxLength = (MaxLength)Attribute.GetCustomAttribute(prop, typeof(MaxLength));
                            var value = prop.GetValue(model);
                            if (value != null)
                            {
                                var str = value.ToString();
                                if (str.Length > maxLength.length)
                                {
                                    error =
                                        $"{model.GetType().FullName}.{prop.Name} length is longer than {maxLength.length}. SaveChanges() has been terminated.";
                                    break;
                                }
                            }
                        }
                    }

                    if (error != null) break;
                }
            }

            return error;
        }

        private async Task<IReadOnlyDictionary<string, Metadata>> LoadMetadataList(StorageContext context,
            IEnumerable<PropertyInfo> storageSets, Type contextType)
        {
            var map = new Dictionary<string, Metadata>();
            foreach (var prop in storageSets)
            {
                var modelType = prop.PropertyType.GetGenericArguments()[0];
                var storageTableName = Util.GetStorageTableName(contextType, modelType);
                var metadata = await _storageManagerUtil.LoadMetadata(storageTableName) ?? new Metadata
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
            foreach (var prop in storageSets)
            {
                var storageSetValue = prop.GetValue(context);
                var modelType = prop.PropertyType.GetGenericArguments()[0];
                var storageTableName = Util.GetStorageTableName(contextType, modelType);

                EnsureAllModelsHaveIds(storageSetValue, modelType, metadataMap);
                EnsureAllAssociationsHaveIds(context, storageSetValue, modelType, storageSets, metadataMap);

                var guids = await SaveModels(storageSetValue, modelType, storageTableName, storageSets);
                total += guids.Count;
                var oldMetadata = metadataMap[Util.GetFullyQualifiedTypeName(modelType)];
                SaveMetadata(storageTableName, guids, contextType, modelType, oldMetadata);
                DeleteOldModelsFromStorage(oldMetadata, storageTableName);
                _logger.StorageSetSaved(modelType, guids.Count);
            }

            return total;
        }

        private void EnsureAllAssociationsHaveIds(StorageContext context, object storageSetValue, Type modelType,
            List<PropertyInfo> storageSets, IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            var storageSetType = StorageManagerUtil.GenericStorageSetType.MakeGenericType(modelType);
            var method = storageSetType.GetMethod(StorageManagerUtil.GetEnumerator);
            var enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                var model = enumerator.Current;
                foreach (var prop in model.GetType().GetProperties())
                {
                    if (prop.GetValue(model) == null || !_storageManagerUtil.IsInContext(storageSets, prop) &&
                        !_storageManagerUtil.IsListInContext(storageSets, prop)) continue;
                    if (_storageManagerUtil.IsInContext(storageSets, prop))
                        EnsureOneAssociationHasId(context, prop.GetValue(model), prop.PropertyType, storageSets,
                            metadataMap);
                    if (_storageManagerUtil.IsListInContext(storageSets, prop))
                        EnsureManyAssociationHasId(context, prop.GetValue(model), prop, storageSets, metadataMap);
                }
            }
        }

        private static void EnsureManyAssociationHasId(StorageContext context, object listObject, PropertyInfo prop,
            List<PropertyInfo> storageSets, IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            var method = listObject.GetType().GetMethod(StorageManagerUtil.GetEnumerator);
            var enumerator = (IEnumerator)method.Invoke(listObject, new object[] { });
            while (enumerator.MoveNext())
            {
                var model = enumerator.Current;
                EnsureOneAssociationHasId(context, model, prop.PropertyType.GetGenericArguments()[0], storageSets,
                    metadataMap);
            }
        }

        private static void EnsureOneAssociationHasId(StorageContext context, object associatedModel, Type propType,
            List<PropertyInfo> storageSets, IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            var idProp = GetIdProperty(associatedModel);
            var id = Convert.ToString(idProp.GetValue(associatedModel));
            var metadata = metadataMap[Util.GetFullyQualifiedTypeName(propType)];
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
            var q = from p in storageSets
                    where p.PropertyType.GetGenericArguments()[0] == propType
                    select p;
            var storeageSetProp = q.Single();
            var storeageSet = storeageSetProp.GetValue(context);
            var listProp = storeageSet.GetType().GetProperty(StorageManagerUtil.List, StorageManagerUtil.Flags);
            var list = listProp.GetValue(storeageSet);
            var method = list.GetType().GetMethod(StorageManagerUtil.GetEnumerator);
            var enumerator = (IEnumerator)method.Invoke(list, new object[] { });
            var found = false;
            while (enumerator.MoveNext())
            {
                var model = enumerator.Current;
                if (id != GetId(model)) continue;
                found = true;
                break;
            }

            if (!found)
                throw new InvalidOperationException(
                    $"A model of type: {propType.Name} with Id {id} was deleted but still being used by an association. Remove it from the association as well.");
        }

        private static void EnsureAllModelsHaveIds(object storageSetValue, Type modelType,
            IReadOnlyDictionary<string, Metadata> metadataMap)
        {
            var storageSetType = StorageManagerUtil.GenericStorageSetType.MakeGenericType(modelType);
            var method = storageSetType.GetMethod(StorageManagerUtil.GetEnumerator);
            var metadata = metadataMap[Util.GetFullyQualifiedTypeName(modelType)];
            var enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                var model = enumerator.Current;
                if (GetId(model) != 0) continue;
                metadata.MaxId = metadata.MaxId + 1;
                SetId(model, metadata.MaxId);
            }
        }

        private async void SaveMetadata(string storageTableName, List<Guid> guids, Type context, Type modelType,
            Metadata oldMetadata)
        {
            var metadata = new Metadata
            {
                Guids = guids,
                ContextName = Util.GetFullyQualifiedTypeName(context),
                ModelName = Util.GetFullyQualifiedTypeName(modelType),
                MaxId = oldMetadata.MaxId
            };
            var name = $"{storageTableName}-{StorageManagerUtil.Metadata}";
            await _blazorDBInterop.SetItem(name, Json.Serialize(metadata), false);
        }

        private async Task<List<Guid>> SaveModels(object storageSetValue, Type modelType, string storageTableName,
            List<PropertyInfo> storageSets)
        {
            var guids = new List<Guid>();
            var storageSetType = StorageManagerUtil.GenericStorageSetType.MakeGenericType(modelType);
            var method = storageSetType.GetMethod(StorageManagerUtil.GetEnumerator);
            var enumerator = (IEnumerator)method.Invoke(storageSetValue, new object[] { });
            while (enumerator.MoveNext())
            {
                var guid = Guid.NewGuid();
                guids.Add(guid);
                var model = enumerator.Current;
                var name = $"{storageTableName}-{guid}";
                var serializedModel = ScanModelForAssociations(model, storageSets, Json.Serialize(model));
                await _blazorDBInterop.SetItem(name, serializedModel, false);
            }

            return guids;
        }

        private void DeleteOldModelsFromStorage(Metadata metadata, string storageTableName)
        {
            foreach (var guid in metadata.Guids)
            {
                var name = $"{storageTableName}-{guid}";
                _blazorDBInterop.RemoveItem(name, false);
            }
        }

        private string ScanModelForAssociations(object model, List<PropertyInfo> storageSets,
            string serializedModel)
        {
            var result = serializedModel;
            foreach (var prop in model.GetType().GetProperties())
            {
                if (prop.GetValue(model) == null || !_storageManagerUtil.IsInContext(storageSets, prop) &&
                    !_storageManagerUtil.IsListInContext(storageSets, prop)) continue;
                if (_storageManagerUtil.IsInContext(storageSets, prop)) result = FixOneAssociation(model, prop, result);
                if (_storageManagerUtil.IsListInContext(storageSets, prop))
                    result = FixManyAssociation(model, prop, result);
            }

            return result;
        }

        private static string FixManyAssociation(object model, PropertyInfo prop, string result)
        {
            var modelList = (IEnumerable)prop.GetValue(model);
            foreach (var item in modelList)
            {
                var idProp = GetIdProperty(item);
                var id = Convert.ToString(idProp.GetValue(item));
                var serializedItem = Json.Serialize(item);
                result = ReplaceModelWithId(result, serializedItem, id);
            }

            return result;
        }

        private static string FixOneAssociation(object model, PropertyInfo prop, string result)
        {
            var associatedModel = prop.GetValue(model);
            var idProp = GetIdProperty(associatedModel);
            var id = Convert.ToString(idProp.GetValue(associatedModel));
            var serializedItem = Json.Serialize(associatedModel);
            result = ReplaceModelWithId(result, serializedItem, id);
            return result;
        }

        private static int SaveAssociationModel(StorageContext context, object associatedModel, Type propType,
            IEnumerable<PropertyInfo> storageSets, int id)
        {
            var q = from p in storageSets
                    where p.PropertyType.GetGenericArguments()[0] == propType
                    select p;
            var storeageSetProp = q.Single();
            var storeageSet = storeageSetProp.GetValue(context);
            var listProp = storeageSet.GetType().GetProperty(StorageManagerUtil.List, StorageManagerUtil.Flags);
            var list = listProp.GetValue(storeageSet);
            var addMethod = list.GetType().GetMethod(StorageManagerUtil.Add);
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
            var prop = GetIdProperty(item);
            return (int)prop.GetValue(item);
        }

        private static void SetId(object item, int id)
        {
            var prop = GetIdProperty(item);
            prop.SetValue(item, id);
        }

        private static PropertyInfo GetIdProperty(object item)
        {
            var prop = item.GetType().GetProperty(StorageManagerUtil.Id);
            if (prop == null) throw new ArgumentException("Model must have an Id property");
            return prop;
        }
    }
}