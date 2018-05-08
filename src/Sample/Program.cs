using BlazorDB;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;

namespace Sample
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
            });
            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
}
