using Blazor.Fluxor;

namespace FluxorIntegration.Store.Reviews
{
    public class DeleteReviewAction : IAction
    {
        public int MovieId { get; private set; }
        public int ReviewId { get; private set; }

        public DeleteReviewAction(int movieId, int reviewId)
        {
            MovieId = movieId;
            ReviewId = reviewId;
        }
    }
}
