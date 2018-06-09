using System;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Blazor.Fluxor;
using BlazorDB;
using FluxorIntegration.Models;
using FluxorIntegration.Store.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace FluxorIntegration
{
    public class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new BrowserServiceProvider(services =>
            {
                services.AddBlazorDB(options =>
                {
                    options.LogDebug = true;
                    options.Assembly = typeof(Program).Assembly;
                });

                services.AddFluxor(options => options
                    .UseDependencyInjection(typeof(Program).Assembly)
                    .AddMiddleware<Blazor.Fluxor.ReduxDevTools.ReduxDevToolsMiddleware>()
                    .AddMiddleware<Blazor.Fluxor.Routing.RoutingMiddleware>()
                    .AddMiddleware<BlazorDBFluxorMiddleware<Context>>()
                );
                //services.AddSingleton(typeof(IState<BlazorDBStorageSetState<Movie>>), typeof(State<BlazorDBStorageSetState<Movie>>));
                foreach (var service in services)
                {
                    Console.WriteLine("Service: {0}", service.ServiceType);
                }
            });

            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
}
