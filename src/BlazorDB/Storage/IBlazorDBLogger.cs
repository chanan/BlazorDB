using System;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    public interface IBlazorDBLogger
    {
        Task ContextSaved(Type contextType);
        void EndGroup();
        void ItemAddedToContext(string contextTypeName, Type modelType, object item);
        void ItemRemovedFromContext(string contextTypeName, Type modelType);
        void LoadModelInContext(Type modelType, int count);
        Task LogStorageSetToConsole(Type type, object list);
        Task StartContextType(Type contextType, bool loading = true);
        void StorageSetSaved(Type modelType, int count);
        void Error(string error);
    }
}