using BlazorDB;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using System;

namespace Sample
{
    public class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new BrowserServiceProvider(services =>
            {
                services.AddBlazorDB(typeof(Program).Assembly);
            });
            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
}
