using BlazorDB;

namespace Sample.Models
{
    public class Context : StorageContext
    {
        public StorageSet<Person> People { get; set; }
        public StorageSet<Address> Addresses { get; set; }
    }
}
