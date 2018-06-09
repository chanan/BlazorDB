using System;
using System.Collections.Generic;
using Blazor.Fluxor;
using BlazorLogger;
using Microsoft.Extensions.DependencyInjection;

namespace FluxorIntegration.Store.Middleware
{
    public class BlazorDBFluxorMiddleware<TContext> : Blazor.Fluxor.Middleware where TContext : BlazorDB.StorageContext
    {
        private readonly TContext _context;
        private IStore _store;
        private IDictionary<string, string> _properties = new Dictionary<string, string>();
        public BlazorDBFluxorMiddleware(TContext context)
        {
            _context = context;
        }
        public override void Initialize(IStore store)
        {
            base.Initialize(store);
            _store = store;
            Console.WriteLine("The Middleware has just been initialized, we now have a reference to the store");
            _context.LogToConsole();
            foreach (var prop in _context.GetType().GetProperties())
            {
                var list = prop.GetValue(_context);
                var modelType = prop.PropertyType.GetGenericArguments()[0];
                _properties.Add(modelType.FullName, prop.Name);
                var featureType = typeof(BlazorDBStorageSetFeature<>).MakeGenericType(modelType);
                var feature = (IFeature)Activator.CreateInstance(featureType);
                _store.AddFeature(feature);
                var actionType = typeof(LoadStorageSetAction<>).MakeGenericType(modelType);
                var action = (IAction)Activator.CreateInstance(actionType, new object[] { modelType, list });
                _store.DispatchAsync(action);
            }
        }

        public override void BeforeDispatch(IAction action)
        {
            Console.WriteLine("BeforeDispatch action: {0}", action);
            if (action.GetType().IsGenericType)
            {
                if (action.GetType().GetGenericTypeDefinition() == typeof(LoadStorageSetAction<>))
                {
                    var modelType = action.GetType().GetGenericArguments()[0];
                    Console.WriteLine("Generic: {0}", modelType.FullName);
                    foreach (var feature in Store.Features)
                    {
                        if (feature.GetName() == modelType.FullName)
                        {
                            var propName = _properties[feature.GetName()];
                            var list = _context.GetType().GetProperty(propName).GetValue(_context);
                            var stateType = typeof(BlazorDBStorageSetState<>).MakeGenericType(modelType);
                            var state = Activator.CreateInstance(stateType, list);
                            feature.RestoreState(state);
                        }
                    }
                }
            }
        }
    }
}