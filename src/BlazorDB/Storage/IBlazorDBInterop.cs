using System.Threading.Tasks;

namespace BlazorDB.Storage
{
    internal interface IBlazorDBInterop
    {
        Task<bool> Clear(bool session);
        Task<string> GetItem(string key, bool session);
        Task<bool> Log(params object[] list);
        Task<bool> RemoveItem(string key, bool session);
        Task<bool> SetItem(string key, string value, bool session);
    }
}