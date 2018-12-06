using Microsoft.Extensions.DependencyInjection;

namespace FluxorIntegration.Pete
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorFluxor<TContext>(this IServiceCollection services)
            where TContext: class, new()
        {
            var context = new TContext();
            services.AddSingleton(context);
            return services;
        }
    }
}