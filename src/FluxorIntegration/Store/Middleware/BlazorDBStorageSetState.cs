using System;
using System.Collections.Generic;

namespace FluxorIntegration.Store.Middleware
{
    public class BlazorDBStorageSetState<T>
    {
        public IList<T> List { get; private set; }
        [Obsolete("For deserialization purposes only. Use the constructor with parameters")]
        public BlazorDBStorageSetState() { }

        public BlazorDBStorageSetState(IList<T> list)
        {
            List = list;
        }
    }
}
