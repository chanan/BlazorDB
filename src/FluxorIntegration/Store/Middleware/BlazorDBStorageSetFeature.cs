using System.Collections.Generic;
using Blazor.Fluxor;

namespace FluxorIntegration.Store.Middleware
{
    public class BlazorDBStorageSetFeature<T> : Feature<BlazorDBStorageSetState<T>>
    {
        public override string GetName() => typeof(T).FullName;

        protected override BlazorDBStorageSetState<T> GetInitialState()
        {
            return new BlazorDBStorageSetState<T>(new List<T>());
        }
    }
}
