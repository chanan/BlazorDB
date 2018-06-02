using System.Collections.Generic;

namespace FluxorIntegration.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Review> Reviews { get; set; }
    }
}
