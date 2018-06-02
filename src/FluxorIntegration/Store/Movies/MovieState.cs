using System;
using System.Collections.Generic;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Movies
{
    public class MovieState
    {
        public IList<Movie> Movies { get; private set; }

        [Obsolete("For deserialization purposes only. Use the constructor with parameters")]
        public MovieState() { }
        public MovieState(IList<Movie> movies)
        {
            Movies = movies;
        }
    }
}
