using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal class BlazorDBInterop : IBlazorDBInterop
    {
        private readonly IJSRuntime _jsRuntime;

        public BlazorDBInterop(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }
        public Task<bool> SetItem(string key, string value, bool session)
        {
            return _jsRuntime.InvokeAsync<bool>("blazorDBInterop.setItem", key, value, session);
        }

        public Task<string> GetItem(string key, bool session)
        {
            return _jsRuntime.InvokeAsync<string>("blazorDBInterop.getItem", key, session);
        }

        public Task<bool> RemoveItem(string key, bool session)
        {
            return _jsRuntime.InvokeAsync<bool>("blazorDBInterop.removeItem", key, session);
        }

        public Task<bool> Clear(bool session)
        {
            return _jsRuntime.InvokeAsync<bool>("blazorDBInterop.clear", session);
        }
        public Task<bool> Log(params object[] list)
        {
            List<object> _list = new List<object>(list); //This line is needed see: https://github.com/aspnet/Blazor/issues/740
            return _jsRuntime.InvokeAsync<bool>("blazorDBInterop.logs");
        }
    }
}
