using Blazor.Fluxor;
using BlazorDB;
using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FluxorIntegration
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
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
        }

        public void Configure(IBlazorApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
