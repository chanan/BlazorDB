using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorDB.Storage
{
    internal class BlazorDBInterop
    {
        public static Task<bool> SetItem(string key, string value, bool session)
        {
            return JSRuntime.Current.InvokeAsync<bool>("blazorDBInterop.setItem", key, value, session);
        }

        public static Task<string> GetItem(string key, bool session)
        {
            return JSRuntime.Current.InvokeAsync<string>("blazorDBInterop.getItem", key, session);
        }

        public static Task<bool> RemoveItem(string key, bool session)
        {
            return JSRuntime.Current.InvokeAsync<bool>("blazorDBInterop.removeItem", key, session);
        }

        public static Task<bool> Clear(bool session)
        {
            return JSRuntime.Current.InvokeAsync<bool>("blazorDBInterop.clear", session);
        }
        public static Task<bool> Log(params object[] list)
        {
            var _list = new List<object>(list); //This line is needed see: https://github.com/aspnet/Blazor/issues/740
            return JSRuntime.Current.InvokeAsync<bool>("blazorDBInterop.logs");
        }
    }
}
