using System;
using System.Collections.Generic;
using Blazor.Fluxor;

namespace FluxorIntegration.Store.Middleware
{
    public class LoadStorageSetAction<T> : IAction
    {
        public Type ModelType { get; private set; }
        public IList<T> List { get; private set; }

        public LoadStorageSetAction(Type modelType, IList<T> list)
        {
            ModelType = modelType;
            List = list;
        }
    }
}
