using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Movies
{
    public class LoadMoviesReducer : IReducer<MovieState, LoadMoviesAction>
    {
        private Context Context { get; set; }

        public LoadMoviesReducer(Context context)
        {
            Context = context;
        }
        public MovieState Reduce(MovieState state, LoadMoviesAction action)
        {
            return new MovieState(Context.Movies.ToList());
        }
    }
}
