using BlazorDB;

namespace FluxorIntegration.Models
{
    public class Context : StorageContext
    {
        public StorageSet<Movie> Movies { get; set; }
        public StorageSet<Review> Reviews { get; set; }
    }
}
