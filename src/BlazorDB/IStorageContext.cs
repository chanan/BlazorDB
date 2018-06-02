namespace BlazorDB
{
    public interface IStorageContext
    {
        int SaveChanges();
        void LogToConsole();
    }
}
