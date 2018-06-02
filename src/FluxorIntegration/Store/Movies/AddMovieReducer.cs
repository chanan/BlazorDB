using System.Linq;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Movies
{
    public class AddMovieReducer : IReducer<MovieState, AddMovieAction>
    {
        private Context Context { get; set; }

        public AddMovieReducer(Context context)
        {
            Context = context;
        }

        public MovieState Reduce(MovieState state, AddMovieAction action)
        {
            var movie = new Movie {Name = action.Name};
            Context.Movies.Add(movie);
            Context.SaveChanges();
            return new MovieState(Context.Movies.ToList());
        }
    }
}
