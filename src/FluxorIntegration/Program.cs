using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Blazor.Fluxor;
using BlazorDB;

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
                );
            });

            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
}
