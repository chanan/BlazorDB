using System;
using Microsoft.AspNetCore.Blazor.Browser.Interop;

namespace BlazorDB
{
    internal class BlazorDBInterop
    {
        public static bool SetItem(string key, string value, bool session)
        {
            return RegisteredFunction.Invoke<bool>("BlazorDB.blazorDBInterop.SetItem", key, value, session);
        }

        public static string GetItem(string key, bool session)
        {
            return RegisteredFunction.Invoke<string>("BlazorDB.blazorDBInterop.GetItem", key, session);
        }

        public static bool RemoveItem(string key, bool session)
        {
            return RegisteredFunction.Invoke<bool>("BlazorDB.blazorDBInterop.RemoveItem", key, session);
        }

        public static bool Clear(bool session)
        {
            return RegisteredFunction.Invoke<bool>("BlazorDB.blazorDBInterop.Clear", session);
        }
    }
}
