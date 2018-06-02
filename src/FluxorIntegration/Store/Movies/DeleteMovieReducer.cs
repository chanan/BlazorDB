using System.Linq;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Movies
{
    public class DeleteMovieReducer : IReducer<MovieState, DeleteMovieAction>
    {
        private Context Context { get; set; }

        public DeleteMovieReducer(Context context)
        {
            Context = context;
        }
        public MovieState Reduce(MovieState state, DeleteMovieAction action)
        {
            var movie = (from m in Context.Movies
                where m.Id == action.MovieId
                select m).Single();
            Context.Movies.Remove(movie);
            Context.SaveChanges();
            return new MovieState(Context.Movies.ToList());
        }
    }
}
