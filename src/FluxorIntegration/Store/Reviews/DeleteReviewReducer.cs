using System.Linq;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Reviews
{
    public class DeleteReviewReducer : IReducer<ReviewsState, DeleteReviewAction>
    {
        private Context Context { get; set; }

        public DeleteReviewReducer(Context context)
        {
            Context = context;
        }
        public ReviewsState Reduce(ReviewsState state, DeleteReviewAction action)
        {
            var movie = (from m in Context.Movies
                where m.Id == action.MovieId
                select m).Single();
            var review = (from r in Context.Reviews
                where r.Id == action.ReviewId
                select r).Single();
            movie.Reviews.RemoveAt(movie.Reviews.FindIndex(r => r.Id == action.ReviewId));
            Context.Reviews.Remove(review);
            Context.SaveChanges();
            return new ReviewsState(action.MovieId, movie.Reviews);
        }
    }
}
