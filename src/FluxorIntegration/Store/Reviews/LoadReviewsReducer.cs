using System.Linq;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Reviews
{
    public class LoadReviewsReducer : IReducer<ReviewsState, LoadReviewsAction>
    {
        private Context Context { get; set; }

        public LoadReviewsReducer(Context context)
        {
            Context = context;
        }
        public ReviewsState Reduce(ReviewsState state, LoadReviewsAction action)
        {
            var query = from movie in Context.Movies
                where movie.Id == action.MovieId
                select movie.Reviews;
            return new ReviewsState(action.MovieId, query.Single());
        }
    }
}
